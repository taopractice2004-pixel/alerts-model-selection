using AlertService.API.Mappings;
using AlertService.API.Exceptions;
using AlertService.API.Options;
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
    private static readonly Severity[] TrendSeverityOrder =
    {
        Severity.Low,
        Severity.Medium,
        Severity.High,
        Severity.Critical
    };

    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;
    private readonly AlertDuplicateSuppressionOptions _duplicateSuppressionOptions;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<AlertDuplicateSuppressionOptions> duplicateSuppressionOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        ArgumentNullException.ThrowIfNull(duplicateSuppressionOptions);
        _duplicateSuppressionOptions = duplicateSuppressionOptions.Value;
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
            request.Tag,
            request.Search,
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
        var startUtc = todayUtc.AddDays(1 - request.Days);
        var endUtcExclusive = todayUtc.AddDays(1);

        var groupedCounts = await _repository.GetDailySeverityCountsAsync(startUtc, endUtcExclusive, cancellationToken);
        var countsByDay = groupedCounts
            .GroupBy(item => DateOnly.FromDateTime(item.DayUtc))
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.Severity, item => item.Count));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);

        for (var offset = 0; offset < request.Days; offset++)
        {
            var day = DateOnly.FromDateTime(startUtc.AddDays(offset));
            countsByDay.TryGetValue(day, out var dayCounts);

            var severityCounts = new AlertSeverityCountsResponse
            {
                Low = GetSeverityCount(dayCounts, Severity.Low),
                Medium = GetSeverityCount(dayCounts, Severity.Medium),
                High = GetSeverityCount(dayCounts, Severity.High),
                Critical = GetSeverityCount(dayCounts, Severity.Critical)
            };

            buckets.Add(new AlertTrendBucketResponse
            {
                Day = day,
                TotalCount = TrendSeverityOrder.Sum(severity => GetSeverityCount(dayCounts, severity)),
                SeverityCounts = severityCounts
            });
        }

        return buckets;
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var alert = request.ToEntity(createdAtUtc);
        var suppressionCutoffUtc = createdAtUtc.AddMinutes(-_duplicateSuppressionOptions.DuplicateSuppressionWindowMinutes);

        var duplicate = await _repository.FindRecentActiveDuplicateAsync(
            alert.Title,
            alert.Severity,
            suppressionCutoffUtc,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title {Title} with severity {Severity} using existing alert {AlertId}",
                alert.Title,
                alert.Severity,
                duplicate.Id);

            return new CreateAlertResult
            {
                Alert = duplicate.ToResponse(),
                DuplicateSuppressed = true
            };
        }

        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult
        {
            Alert = created.ToResponse(),
            DuplicateSuppressed = false
        };
    }

    public async Task<AlertResponse?> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedTags = NormalizeRequestedTags(request.Tags);
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var existingTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.NormalizedName)
            .ToHashSet(StringComparer.Ordinal);

        var newTagCount = requestedTags.Count(tag => !existingTags.Contains(NormalizeTag(tag)));
        if (existingTags.Count + newTagCount > AlertConstants.MaxTagsPerAlert)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [nameof(AddAlertTagsRequest.Tags)] = new[]
                {
                    $"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags."
                }
            });
        }

        var updated = await _repository.AddTagsAsync(id, requestedTags, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", requestedTags.Count, id);
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

    public async Task<AlertResponse?> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeSingleTag(tag);
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return null;
        }

        var updated = await _repository.RemoveTagAsync(id, normalizedTag, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: tag assignment not found", id);
            return null;
        }

        _logger.LogInformation("Removed a tag from alert {AlertId}", id);
        return updated.ToResponse();
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

    private static IReadOnlyList<string> NormalizeRequestedTags(IReadOnlyList<string> tags)
    {
        if (tags is null || tags.Count == 0)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [nameof(AddAlertTagsRequest.Tags)] = new[] { "At least one tag is required." }
            });
        }

        var normalizedTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            var trimmedTag = ValidateAndTrimTag(tag, nameof(AddAlertTagsRequest.Tags));
            var normalizedTag = NormalizeTag(trimmedTag);

            if (seenTags.Add(normalizedTag))
            {
                normalizedTags.Add(trimmedTag);
            }
        }

        return normalizedTags;
    }

    private static string NormalizeSingleTag(string tag)
    {
        var trimmedTag = ValidateAndTrimTag(tag, "tag");
        return NormalizeTag(trimmedTag);
    }

    private static string ValidateAndTrimTag(string? tag, string memberName)
    {
        var trimmedTag = tag?.Trim() ?? string.Empty;
        if (trimmedTag.Length == 0)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [memberName] = new[] { "Tag must not be blank." }
            });
        }

        if (trimmedTag.Length > AlertConstants.TagMaxLength)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [memberName] = new[]
                {
                    $"Tag must be between 1 and {AlertConstants.TagMaxLength} characters after trimming."
                }
            });
        }

        return trimmedTag;
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }

    private static int GetSeverityCount(IReadOnlyDictionary<Severity, int>? dayCounts, Severity severity)
    {
        if (dayCounts is null)
        {
            return 0;
        }

        return dayCounts.TryGetValue(severity, out var count) ? count : 0;
    }
}
