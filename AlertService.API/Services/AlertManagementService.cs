using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;
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

        var buckets = Enumerable.Range(0, request.Days)
            .Select(offset =>
            {
                var day = firstDay.AddDays(offset);
                var dayCounts = countsByDay[day].ToList();
                int CountFor(Severity severity) => dayCounts.Where(c => c.Severity == severity).Sum(c => c.Count);

                return new AlertTrendBucketResponse
                {
                    Date = DateOnly.FromDateTime(day),
                    TotalCount = dayCounts.Sum(c => c.Count),
                    SeverityCounts = new AlertSeverityCountsResponse
                    {
                        Low = CountFor(Severity.Low),
                        Medium = CountFor(Severity.Medium),
                        High = CountFor(Severity.High),
                        Critical = CountFor(Severity.Critical)
                    }
                };
            })
            .ToList();

        return new AlertTrendsResponse { Days = request.Days, Buckets = buckets };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var alert = request.ToEntity(now);

        if (_suppressionOptions.DuplicateWindowMinutes > 0)
        {
            var windowStart = now.AddMinutes(-_suppressionOptions.DuplicateWindowMinutes);
            var duplicate = await _repository.FindRecentActiveDuplicateAsync(alert.Title, alert.Severity, windowStart, cancellationToken);
            if (duplicate is not null)
            {
                _logger.LogInformation("Suppressed duplicate alert; returning existing alert {AlertId}", duplicate.Id);
                return new CreateAlertResult(duplicate.ToResponse(), true);
            }
        }

        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(created.ToResponse(), false);
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

    public async Task<AddTagsResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot tag alert {AlertId}: not found", id);
            return new AddTagsResult(AddTagsStatus.AlertNotFound);
        }

        var assigned = alert.Tags.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var newNames = request.Tags
            .Select(Tag.Normalize)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !assigned.Contains(name))
            .ToList();

        if (assigned.Count + newNames.Count > AlertConstants.MaxTagsPerAlert)
        {
            return new AddTagsResult(AddTagsStatus.TagLimitExceeded);
        }

        if (newNames.Count > 0)
        {
            await _repository.AddTagsAsync(alert, newNames, cancellationToken);
            _logger.LogInformation("Added {TagCount} tags to alert {AlertId}", newNames.Count, id);
        }

        return new AddTagsResult(AddTagsStatus.Added, alert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        var normalizedTag = Tag.Normalize(tag);
        if (alert is null || !await _repository.RemoveTagAsync(alert, normalizedTag, cancellationToken))
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: not found", normalizedTag, id);
            return false;
        }

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", normalizedTag, id);
        return true;
    }
}
