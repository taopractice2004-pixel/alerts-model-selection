using AlertService.API.Configuration;
using AlertService.API.Mappings;
using AlertService.Common.Constants;
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
    private readonly ILogger<AlertManagementService> _logger;
    private readonly AlertSuppressionOptions _suppressionOptions;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        IOptions<AlertSuppressionOptions> suppressionOptions)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _suppressionOptions = suppressionOptions.Value;
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

    public async Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var oldestDay = today.AddDays(-(request.Days - 1));
        var fromInclusiveUtc = oldestDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var dailyCounts = await _repository.GetDailyTrendsAsync(fromInclusiveUtc, toExclusiveUtc, cancellationToken);
        var countsByDay = dailyCounts.ToDictionary(count => DateOnly.FromDateTime(count.Day));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var day = oldestDay; day <= today; day = day.AddDays(1))
        {
            countsByDay.TryGetValue(day, out var counts);
            buckets.Add(new AlertTrendBucketResponse
            {
                Date = day,
                TotalCount = counts.LowCount + counts.MediumCount + counts.HighCount + counts.CriticalCount,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = counts.LowCount,
                    Medium = counts.MediumCount,
                    High = counts.HighCount,
                    Critical = counts.CriticalCount
                }
            });
        }

        _logger.LogInformation("Computed alert trends for {BucketCount} UTC day(s) in range [{From:o}, {To:o})", buckets.Count, fromInclusiveUtc, toExclusiveUtc);
        return new AlertTrendsResponse { Buckets = buckets };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddMinutes(-_suppressionOptions.WindowMinutes);
        var duplicate = await _repository.FindActiveDuplicateAsync(request.Title, request.Severity, windowStart, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert for title {Title} with severity {Severity}; returning existing alert {AlertId}", duplicate.Title, duplicate.Severity, duplicate.Id);
            return CreateAlertResult.SuppressedDuplicate(duplicate.ToResponse());
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return CreateAlertResult.Created(created.ToResponse());
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
            return AddTagsResult.AlertNotFound();
        }

        var existingNames = new HashSet<string>(alert.Tags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var newTagNames = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in request.Tags)
        {
            var trimmed = raw.Trim();
            if (existingNames.Contains(trimmed) || !seen.Add(trimmed))
            {
                continue;
            }

            newTagNames.Add(trimmed);
        }

        if (existingNames.Count + newTagNames.Count > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: would exceed {MaxTags} tags", id, AlertConstants.MaxTagsPerAlert);
            return AddTagsResult.TagLimitExceeded();
        }

        if (newTagNames.Count > 0)
        {
            await _repository.AddTagsToAlertAsync(alert, newTagNames, cancellationToken);
        }

        _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", newTagNames.Count, id);
        return AddTagsResult.Success(alert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        var assigned = alert.Tags.FirstOrDefault(t => string.Equals(t.Name, tag.Trim(), StringComparison.OrdinalIgnoreCase));
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag '{Tag}' from alert {AlertId}: not assigned", tag, id);
            return false;
        }

        alert.Tags.Remove(assigned);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag '{Tag}' from alert {AlertId}", assigned.Name, id);
        return true;
    }
}
