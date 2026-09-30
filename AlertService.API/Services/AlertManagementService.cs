using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IConfiguration configuration,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _configuration = configuration;
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

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var startDate = today.AddDays(-(request.Days - 1));
        var startInclusiveUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusiveUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var counts = await _repository.GetTrendCountsAsync(startInclusiveUtc, endExclusiveUtc, cancellationToken);
        var countsByDate = counts.ToLookup(c => DateOnly.FromDateTime(c.Date));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            var severityCounts = new AlertSeverityCountsResponse();
            var totalCount = 0;
            foreach (var count in countsByDate[date])
            {
                totalCount += count.Count;
                switch (count.Severity)
                {
                    case Severity.Low:
                        severityCounts.Low = count.Count;
                        break;
                    case Severity.Medium:
                        severityCounts.Medium = count.Count;
                        break;
                    case Severity.High:
                        severityCounts.High = count.Count;
                        break;
                    case Severity.Critical:
                        severityCounts.Critical = count.Count;
                        break;
                }
            }

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = date,
                TotalCount = totalCount,
                SeverityCounts = severityCounts
            });
        }

        return new AlertTrendsResponse { Buckets = buckets };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var title = request.Title.Trim();
        var windowMinutes = _configuration.GetValue(
            AlertConstants.DuplicateSuppressionWindowMinutesConfigKey,
            AlertConstants.DefaultDuplicateSuppressionWindowMinutes);
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-windowMinutes);

        var duplicate = await _repository.FindRecentActiveDuplicateAsync(title, request.Severity, cutoff, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert creation for title '{Title}' with severity {Severity}; matched existing alert {AlertId}", title, request.Severity, duplicate.Id);
            return new CreateAlertResult(duplicate.ToResponse(), IsDuplicate: true);
        }

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
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

    public async Task<AlertResponse?> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var existingNames = alert.Tags.Select(t => t.Name).ToList();
        var newNames = request.Tags
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !existingNames.Contains(t, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (existingNames.Count + newNames.Count > AlertConstants.MaxTagsPerAlert)
        {
            throw new TagLimitExceededException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (newNames.Count > 0)
        {
            var tags = await _repository.GetOrCreateTagsAsync(newNames, cancellationToken);
            foreach (var newTag in tags)
            {
                alert.Tags.Add(newTag);
            }

            await _repository.UpdateAsync(alert, cancellationToken);
            _logger.LogInformation("Added {Count} tag(s) to alert {AlertId}", newNames.Count, id);
        }

        return alert.ToResponse();
    }

    public async Task<AlertResponse?> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert not found", id);
            return null;
        }

        var existingTag = alert.Tags.FirstOrDefault(t => string.Equals(t.Name, tag, StringComparison.OrdinalIgnoreCase));
        if (existingTag is null)
        {
            _logger.LogWarning("Cannot remove tag '{Tag}' from alert {AlertId}: tag not assigned", tag, id);
            return null;
        }

        alert.Tags.Remove(existingTag);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag '{Tag}' from alert {AlertId}", tag, id);
        return alert.ToResponse();
    }
}
