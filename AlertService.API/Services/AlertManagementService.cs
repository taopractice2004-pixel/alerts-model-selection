using AlertService.API.Configuration;
using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Options;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly AlertSuppressionOptions _suppressionOptions;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<AlertSuppressionOptions> suppressionOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _suppressionOptions = suppressionOptions.Value;
        _logger = logger;
    }

    public async Task<PagedResponse<AlertResponse>> GetAllAsync(AlertQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (alerts, totalCount) = await _repository.GetAllAsync(
            request.IsActive,
            request.Severity,
            request.CreatedFrom,
            request.CreatedTo,
            request.Search,
            request.Tag,
            request.SortBy,
            request.SortDirection,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResponse<AlertResponse>
        {
            Items = alerts.Select(a => a.ToResponse()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<AlertResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Alert {AlertId} not found", id);
            return null;
        }

        return alert.ToResponse();
    }

    public async Task<AlertSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _repository.GetSummaryAsync(cancellationToken);

        return new AlertSummaryResponse
        {
            TotalCount = summary.TotalCount,
            ActiveCount = summary.ActiveCount,
            InactiveCount = summary.InactiveCount,
            SeverityCounts = new AlertSeverityCountsResponse
            {
                Low = summary.LowCount,
                Medium = summary.MediumCount,
                High = summary.HighCount,
                Critical = summary.CriticalCount
            }
        };
    }

    public async Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var firstDay = today.AddDays(-(request.Days - 1));

        var counts = await _repository.GetDailySeverityCountsAsync(firstDay, today.AddDays(1), cancellationToken);
        var countsByDay = counts.ToLookup(c => c.Date.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var day = firstDay; day <= today; day = day.AddDays(1))
        {
            var severityCounts = new AlertSeverityCountsResponse();
            foreach (var (_, severity, count) in countsByDay[day])
            {
                switch (severity)
                {
                    case Severity.Low: severityCounts.Low += count; break;
                    case Severity.Medium: severityCounts.Medium += count; break;
                    case Severity.High: severityCounts.High += count; break;
                    case Severity.Critical: severityCounts.Critical += count; break;
                }
            }

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = DateOnly.FromDateTime(day),
                TotalCount = severityCounts.Low + severityCounts.Medium + severityCounts.High + severityCounts.Critical,
                SeverityCounts = severityCounts
            });
        }

        return new AlertTrendsResponse { Days = request.Days, Buckets = buckets };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddMinutes(-_suppressionOptions.DuplicateWindowMinutes);

        var duplicate = await _repository.FindRecentActiveDuplicateAsync(request.Title.Trim(), request.Severity, windowStart, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert; returning existing alert {AlertId} with severity {Severity}", duplicate.Id, duplicate.Severity);
            return new CreateAlertResult(duplicate.ToResponse(), IsDuplicate: true);
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(created.ToResponse(), IsDuplicate: false);
    }

    public async Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot update alert {AlertId}: not found", id);
            return null;
        }

        alert.ApplyUpdate(request);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Updated alert {AlertId}", id);
        return alert.ToResponse();
    }

    public async Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot deactivate alert {AlertId}: not found", id);
            return null;
        }

        if (!alert.IsActive)
        {
            return alert.ToResponse();
        }

        alert.IsActive = false;
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Deactivated alert {AlertId}", id);
        return alert.ToResponse();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot delete alert {AlertId}: not found", id);
            return false;
        }

        await _repository.DeleteAsync(alert, cancellationToken);

        _logger.LogInformation("Deleted alert {AlertId}", id);
        return true;
    }

    public async Task<AddTagsResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return new AddTagsResult(AddTagsOutcome.AlertNotFound);
        }

        var knownTags = new HashSet<string>(alert.Tags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var newTags = new List<string>();
        foreach (var tag in request.Tags.Select(t => t.Trim()))
        {
            if (knownTags.Add(tag))
            {
                newTags.Add(tag);
            }
        }

        if (knownTags.Count > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: limit of {MaxTags} tags exceeded", id, AlertConstants.MaxTagsPerAlert);
            return new AddTagsResult(AddTagsOutcome.TagLimitExceeded);
        }

        if (newTags.Count > 0)
        {
            await _repository.AddTagsAsync(alert, newTags, cancellationToken);
            _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", newTags.Count, id);
        }

        return new AddTagsResult(AddTagsOutcome.Added, alert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert not found", id);
            return false;
        }

        var assigned = alert.Tags.FirstOrDefault(t => string.Equals(t.Name, tag?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: tag not assigned", id);
            return false;
        }

        await _repository.RemoveTagAsync(alert, assigned, cancellationToken);

        _logger.LogInformation("Removed tag {TagId} from alert {AlertId}", assigned.Id, id);
        return true;
    }
}
