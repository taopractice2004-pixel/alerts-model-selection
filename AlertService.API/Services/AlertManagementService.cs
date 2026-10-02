using AlertService.API.Mappings;
using AlertService.API.Options;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly AlertSuppressionOptions _suppressionOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        IOptions<AlertSuppressionOptions> suppressionOptions,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _suppressionOptions = suppressionOptions.Value;
        _timeProvider = timeProvider;
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

    public async Task<IReadOnlyList<AlertTrendBucketResponse>> GetTrendsAsync(AlertTrendQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startUtcInclusive = todayUtc.AddDays(-(request.Days - 1));
        var endUtcExclusive = todayUtc.AddDays(1);

        var trendCounts = await _repository.GetDailySeverityCountsAsync(startUtcInclusive, endUtcExclusive, cancellationToken);
        var byDay = trendCounts.ToDictionary(entry => entry.DayUtc.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var dayOffset = 0; dayOffset < request.Days; dayOffset++)
        {
            var dayUtc = startUtcInclusive.AddDays(dayOffset);
            byDay.TryGetValue(dayUtc, out var counts);

            var severityCounts = new AlertSeverityCountsResponse
            {
                Low = counts.LowCount,
                Medium = counts.MediumCount,
                High = counts.HighCount,
                Critical = counts.CriticalCount
            };

            buckets.Add(new AlertTrendBucketResponse
            {
                DateUtc = dayUtc,
                TotalCount = severityCounts.Low + severityCounts.Medium + severityCounts.High + severityCounts.Critical,
                SeverityCounts = severityCounts
            });
        }

        return buckets;
    }

    public async Task<(AlertResponse Alert, bool DuplicateSuppressed)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var suppressionFromUtc = nowUtc.AddMinutes(-_suppressionOptions.WindowMinutes);

        var duplicate = await _repository.FindLatestActiveByTitleAndSeverityAsync(
            request.Title,
            request.Severity,
            suppressionFromUtc,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert create and reused alert {AlertId} for severity {Severity}",
                duplicate.Id,
                duplicate.Severity);

            return (duplicate.ToResponse(), true);
        }

        var alert = request.ToEntity(nowUtc);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (created.ToResponse(), false);
    }

    public async Task<AlertResponse?> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags for alert {AlertId}: not found", id);
            return null;
        }

        var normalizedTags = request.Tags
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingTagNames = alert.Tags
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newTagCount = normalizedTags.Count(tag => !existingTagNames.Contains(tag));
        if (alert.Tags.Count + newTagCount > AlertConstants.MaxTagsPerAlert)
        {
            throw new ValidationException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        var updated = await _repository.AddTagsAsync(id, normalizedTags, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot add tags for alert {AlertId}: not found", id);
            return null;
        }

        _logger.LogInformation("Added tags to alert {AlertId}", id);
        return updated.ToResponse();
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

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = tag.Trim();
        if (normalizedTag.Length == 0 || normalizedTag.Length > AlertConstants.TagMaxLength)
        {
            throw new ValidationException($"Tag must be between 1 and {AlertConstants.TagMaxLength} characters.");
        }

        var removed = await _repository.RemoveTagAsync(id, normalizedTag, cancellationToken);

        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert or tag assignment not found", id);
            return false;
        }

        _logger.LogInformation("Removed tag from alert {AlertId}", id);
        return true;
    }
}
