using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Configuration;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private const string DuplicateSuppressionWindowMinutesKey = "AlertDuplicateSuppression:WindowMinutes";

    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;
    private readonly int _duplicateSuppressionWindowMinutes;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        IConfiguration? configuration = null)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _duplicateSuppressionWindowMinutes = Math.Max(configuration?.GetValue<int?>(DuplicateSuppressionWindowMinutesKey) ?? 0, 0);
    }

    public async Task<PagedResponse<AlertResponse>> GetAllAsync(AlertQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (alerts, totalCount) = await _repository.GetAllAsync(
            isActive: request.IsActive,
            severity: request.Severity,
            createdFrom: request.CreatedFrom,
            createdTo: request.CreatedTo,
            search: request.Search,
            tag: request.Tag,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

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

        var utcToday = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var windowStartUtc = utcToday.AddDays(-(request.Days - 1));
        var windowEndUtcExclusive = utcToday.AddDays(1);

        var trendCounts = await _repository.GetDailyTrendsAsync(windowStartUtc, windowEndUtcExclusive, cancellationToken);
        var trendCountsByDate = trendCounts.ToDictionary(trend => trend.DateUtc.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);

        for (var dayOffset = 0; dayOffset < request.Days; dayOffset++)
        {
            var bucketDateUtc = windowStartUtc.AddDays(dayOffset);
            trendCountsByDate.TryGetValue(bucketDateUtc, out var trend);

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = DateTime.SpecifyKind(bucketDateUtc, DateTimeKind.Utc),
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

        return buckets;
    }

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        var result = await CreateWithSuppressionDetailsAsync(request, cancellationToken);
        return result.Alert;
    }

    public async Task<(AlertResponse Alert, bool IsDuplicateSuppressed)> CreateWithSuppressionDetailsAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var createdDateUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var alert = request.ToEntity(createdDateUtc);

        if (_duplicateSuppressionWindowMinutes > 0)
        {
            var suppressionWindowStartUtc = createdDateUtc.AddMinutes(-_duplicateSuppressionWindowMinutes);
            var existingAlert = await _repository.FindActiveDuplicateAsync(
                alert.Title,
                alert.Severity,
                suppressionWindowStartUtc,
                createdDateUtc,
                cancellationToken);

            if (existingAlert is not null)
            {
                _logger.LogInformation(
                    "Suppressed duplicate alert for title {Title} and severity {Severity}; existing alert {AlertId} is still active",
                    existingAlert.Title,
                    existingAlert.Severity,
                    existingAlert.Id);

                return (existingAlert.ToResponse(), true);
            }
        }

        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (created.ToResponse(), false);
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

    public async Task<AlertTagOperationResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTags = NormalizeTags(request.Tags, nameof(AddAlertTagsRequest.Tags));
        if (normalizedTags.Errors.Count > 0)
        {
            return new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.ValidationFailed,
                Errors = normalizedTags.Errors
            };
        }

        var result = await _repository.AddTagsAsync(id, normalizedTags.Tags, cancellationToken);
        return MapTagResult(result);
    }

    public async Task<AlertTagOperationResult> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeTag(tag);
        if (normalizedTag is null)
        {
            return new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.ValidationFailed,
                Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(tag)] = [$"Tag must be between 1 and {AlertConstants.TagMaxLength} characters after trimming."]
                }
            };
        }

        var result = await _repository.RemoveTagAsync(id, normalizedTag, cancellationToken);
        return MapTagResult(result);
    }

    private static AlertTagOperationResult MapTagResult(AlertTagMutationResult result)
    {
        return new AlertTagOperationResult
        {
            Status = result.Status switch
            {
                AlertTagMutationStatus.Success => AlertTagOperationStatus.Success,
                AlertTagMutationStatus.AlertNotFound => AlertTagOperationStatus.AlertNotFound,
                AlertTagMutationStatus.TagAssignmentNotFound => AlertTagOperationStatus.TagAssignmentNotFound,
                AlertTagMutationStatus.ValidationFailed => AlertTagOperationStatus.ValidationFailed,
                _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, "Unsupported tag mutation status.")
            },
            Alert = result.Alert?.ToResponse(),
            Errors = result.Errors
        };
    }

    private static (List<string> Tags, Dictionary<string, string[]> Errors) NormalizeTags(IEnumerable<string?> tags, string memberName)
    {
        var normalizedTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var rawTag in tags)
        {
            var normalizedTag = NormalizeTag(rawTag);
            if (normalizedTag is null)
            {
                errors[memberName] = [$"Each tag must be between 1 and {AlertConstants.TagMaxLength} characters after trimming."];
                break;
            }

            if (!normalizedTags.ContainsKey(normalizedTag))
            {
                normalizedTags[normalizedTag] = rawTag!.Trim();
            }
        }

        if (normalizedTags.Count == 0 && errors.Count == 0)
        {
            errors[memberName] = ["At least one tag is required."];
        }

        return (normalizedTags.Values.ToList(), errors);
    }

    private static string? NormalizeTag(string? tag)
    {
        var trimmedTag = tag?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTag) || trimmedTag.Length > AlertConstants.TagMaxLength)
        {
            return null;
        }

        return trimmedTag.ToUpperInvariant();
    }
}
