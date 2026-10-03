using System.ComponentModel.DataAnnotations;
using AlertService.API.Mappings;
using AlertService.API.Options;
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
    private readonly int _duplicateSuppressionWindowMinutes;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<DuplicateAlertOptions> duplicateAlertOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        ArgumentNullException.ThrowIfNull(duplicateAlertOptions);

        _duplicateSuppressionWindowMinutes = duplicateAlertOptions.Value.DuplicateSuppressionWindowMinutes;
        if (_duplicateSuppressionWindowMinutes <= 0)
        {
            throw new InvalidOperationException("Alerts:DuplicateSuppressionWindowMinutes must be greater than zero.");
        }

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

        var trendCounts = await _repository.GetTrendCountsAsync(startUtcInclusive, endUtcExclusive, cancellationToken);
        var trendCountsByDate = trendCounts.ToDictionary(
            trendCount => DateTime.SpecifyKind(trendCount.Date.Date, DateTimeKind.Utc));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var dayOffset = 0; dayOffset < request.Days; dayOffset++)
        {
            var date = startUtcInclusive.AddDays(dayOffset);
            if (!trendCountsByDate.TryGetValue(date, out var trendCount))
            {
                buckets.Add(CreateTrendBucket(date, 0, 0, 0, 0, 0));
                continue;
            }

            buckets.Add(CreateTrendBucket(
                date,
                trendCount.TotalCount,
                trendCount.LowCount,
                trendCount.MediumCount,
                trendCount.HighCount,
                trendCount.CriticalCount));
        }

        return buckets;
    }

    public async Task<(AlertResponse Alert, bool DuplicateSuppressed)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var normalizedTitle = request.Title.Trim();
        var createdAfterUtc = createdAtUtc.AddMinutes(-_duplicateSuppressionWindowMinutes);

        var duplicate = await _repository.FindActiveDuplicateAsync(
            normalizedTitle,
            request.Severity,
            createdAfterUtc,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert create request and returned alert {AlertId} with severity {Severity}",
                duplicate.Id,
                duplicate.Severity);

            return (duplicate.ToResponse(), true);
        }

        var alert = request.ToEntity(createdAtUtc);
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
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var normalizedTags = NormalizeTags(request.Tags);
        var uniqueTagCount = alert.Tags
            .Select(tag => tag.Name)
            .Concat(normalizedTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (uniqueTagCount > AlertConstants.MaxTagsPerAlert)
        {
            throw new ValidationException(new ValidationResult(
                $"Alerts cannot have more than {AlertConstants.MaxTagsPerAlert} tags.",
                new[] { nameof(AddAlertTagsRequest.Tags) }),
                null,
                request.Tags);
        }

        var updatedAlert = await _repository.AddTagsAsync(alert, normalizedTags, cancellationToken);

        _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", normalizedTags.Count, id);
        return updatedAlert.ToResponse();
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
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        var normalizedTag = tag.Trim();
        var assignedTag = alert.Tags.FirstOrDefault(existingTag => string.Equals(existingTag.Name, normalizedTag, StringComparison.OrdinalIgnoreCase));
        if (assignedTag is null)
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: assignment not found", normalizedTag, id);
            return false;
        }

        await _repository.RemoveTagAsync(alert, normalizedTag, cancellationToken);

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", assignedTag.Name, id);
        return true;
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var normalizedTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            var normalizedTag = tag?.Trim() ?? string.Empty;
            if (normalizedTag.Length == 0)
            {
                throw new ValidationException(new ValidationResult(
                    "Tag values must contain non-whitespace characters.",
                    new[] { nameof(AddAlertTagsRequest.Tags) }),
                    null,
                    tag);
            }

            if (normalizedTag.Length > AlertConstants.TagMaxLength)
            {
                throw new ValidationException(new ValidationResult(
                    $"Tag values cannot exceed {AlertConstants.TagMaxLength} characters.",
                    new[] { nameof(AddAlertTagsRequest.Tags) }),
                    null,
                    tag);
            }

            if (seenTags.Add(normalizedTag))
            {
                normalizedTags.Add(normalizedTag);
            }
        }

        if (normalizedTags.Count == 0)
        {
            throw new ValidationException(new ValidationResult(
                "At least one tag is required.",
                new[] { nameof(AddAlertTagsRequest.Tags) }),
                null,
                tags);
        }

        return normalizedTags;
    }

    private static AlertTrendBucketResponse CreateTrendBucket(
        DateTime date,
        int totalCount,
        int lowCount,
        int mediumCount,
        int highCount,
        int criticalCount)
    {
        return new AlertTrendBucketResponse
        {
            Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            TotalCount = totalCount,
            SeverityCounts = new AlertSeverityCountsResponse
            {
                Low = lowCount,
                Medium = mediumCount,
                High = highCount,
                Critical = criticalCount
            }
        };
    }
}
