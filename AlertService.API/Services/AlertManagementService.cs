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
    private readonly TimeSpan _duplicateSuppressionWindow;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<DuplicateSuppressionOptions> duplicateSuppressionOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _duplicateSuppressionWindow = TimeSpan.FromMinutes(duplicateSuppressionOptions.Value.WindowMinutes);
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

        var utcToday = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startUtcInclusive = utcToday.AddDays(1 - request.Days);
        var endUtcExclusive = utcToday.AddDays(1);
        var trendRows = await _repository.GetDailyTrendsAsync(startUtcInclusive, endUtcExclusive, cancellationToken);
        var trendsByDay = trendRows.ToDictionary(row => row.DayUtc.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var dayOffset = 0; dayOffset < request.Days; dayOffset++)
        {
            var dayUtc = startUtcInclusive.AddDays(dayOffset);
            if (trendsByDay.TryGetValue(dayUtc, out var trend))
            {
                buckets.Add(new AlertTrendBucketResponse
                {
                    Date = dayUtc,
                    TotalCount = trend.TotalCount,
                    SeverityCounts = new AlertSeverityCountsResponse
                    {
                        Low = trend.LowCount,
                        Medium = trend.MediumCount,
                        High = trend.HighCount,
                        Critical = trend.CriticalCount
                    }
                });
            }
            else
            {
                buckets.Add(new AlertTrendBucketResponse
                {
                    Date = dayUtc,
                    TotalCount = 0,
                    SeverityCounts = new AlertSeverityCountsResponse()
                });
            }
        }

        return new AlertTrendsResponse
        {
            Days = request.Days,
            Buckets = buckets
        };
    }

    public async Task<AlertCreateResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var duplicateCutoff = now - _duplicateSuppressionWindow;
        var existingDuplicate = await _repository.GetLatestActiveDuplicateAsync(
            request.Title,
            request.Severity,
            duplicateCutoff,
            cancellationToken);
        if (existingDuplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title '{Title}' and severity {Severity} using alert {AlertId}",
                existingDuplicate.Title,
                existingDuplicate.Severity,
                existingDuplicate.Id);
            return new AlertCreateResult
            {
                Alert = existingDuplicate.ToResponse(),
                IsDuplicateSuppressed = true
            };
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new AlertCreateResult
        {
            Alert = created.ToResponse(),
            IsDuplicateSuppressed = false
        };
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

    public async Task<AlertTagOperationResult> AddTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeTag(tag);
        if (normalizedTag is null)
        {
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.InvalidTag };
        }

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tag to alert {AlertId}: not found", id);
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.AlertNotFound };
        }

        if (alert.Tags.Any(t => string.Equals(t.NormalizedName, normalizedTag, StringComparison.Ordinal)))
        {
            return new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.TagAlreadyAssigned,
                Alert = alert.ToResponse()
            };
        }

        if (alert.Tags.Count >= AlertConstants.MaxTagsPerAlert)
        {
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.MaxTagsReached };
        }

        var tagEntity = await _repository.GetOrCreateTagAsync(tag.Trim(), normalizedTag, cancellationToken);
        alert.Tags.Add(tagEntity);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Added tag '{Tag}' to alert {AlertId}", tagEntity.Name, id);
        return new AlertTagOperationResult
        {
            Status = AlertTagOperationStatus.Success,
            Alert = alert.ToResponse()
        };
    }

    public async Task<AlertTagOperationResult> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeTag(tag);
        if (normalizedTag is null)
        {
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.InvalidTag };
        }

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.AlertNotFound };
        }

        var assignedTag = alert.Tags.FirstOrDefault(t => string.Equals(t.NormalizedName, normalizedTag, StringComparison.Ordinal));
        if (assignedTag is null)
        {
            return new AlertTagOperationResult { Status = AlertTagOperationStatus.TagNotAssigned };
        }

        alert.Tags.Remove(assignedTag);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag '{Tag}' from alert {AlertId}", assignedTag.Name, id);
        return new AlertTagOperationResult
        {
            Status = AlertTagOperationStatus.Success,
            Alert = alert.ToResponse()
        };
    }

    private static string? NormalizeTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var trimmed = tag.Trim();
        if (trimmed.Length is < 1 or > AlertConstants.TagMaxLength)
        {
            return null;
        }

        return trimmed.ToUpperInvariant();
    }
}
