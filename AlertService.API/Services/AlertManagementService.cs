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
            NormalizeOptionalTag(request.Tag),
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

    public async Task<AlertTrendResponse> GetTrendsAsync(AlertTrendQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todayUtc = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var startDateUtc = todayUtc.AddDays(1 - request.Days);
        var buckets = await _repository.GetDailyTrendsAsync(startDateUtc, todayUtc, cancellationToken);

        return new AlertTrendResponse
        {
            Days = request.Days,
            Buckets = buckets.Select(bucket => new AlertTrendBucketResponse
            {
                DateUtc = bucket.DateUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                TotalCount = bucket.TotalCount,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = bucket.LowCount,
                    Medium = bucket.MediumCount,
                    High = bucket.HighCount,
                    Critical = bucket.CriticalCount
                }
            }).ToList()
        };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var duplicateCutoffUtc = nowUtc.AddMinutes(-_suppressionOptions.DuplicateWindowMinutes);
        var existing = await _repository.FindActiveDuplicateAsync(request.Title, request.Severity, duplicateCutoffUtc, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title {Title} and severity {Severity}; returning alert {AlertId}",
                existing.Title,
                existing.Severity,
                existing.Id);
            return new CreateAlertResult(existing.ToResponse(), true);
        }

        var alert = request.ToEntity(nowUtc);
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

    public async Task<AlertResponse?> AddTagsAsync(int id, AssignAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tagsToAssign = PrepareTags(request.Tags);
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var existingTags = alert.Tags
            .Select(tag => tag.NormalizedName)
            .ToHashSet(StringComparer.Ordinal);

        var newTags = tagsToAssign
            .Where(tag => existingTags.Add(NormalizeTag(tag)))
            .ToList();

        if (alert.Tags.Count + newTags.Count > AlertConstants.MaxTagsPerAlert)
        {
            throw new ValidationException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (newTags.Count == 0)
        {
            return alert.ToResponse();
        }

        await _repository.AssignTagsAsync(alert, newTags, cancellationToken);

        _logger.LogInformation("Assigned {TagCount} tags to alert {AlertId}", newTags.Count, id);
        return alert.ToResponse();
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var trimmedTag = ValidateTag(tag);
        var normalizedTag = NormalizeTag(trimmedTag);
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        var existingTag = alert.Tags.FirstOrDefault(t => string.Equals(t.NormalizedName, normalizedTag, StringComparison.Ordinal));
        if (existingTag is null)
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: assignment not found", trimmedTag, id);
            return false;
        }

        alert.Tags.Remove(existingTag);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", trimmedTag, id);
        return true;
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

    private static string? NormalizeOptionalTag(string? tag)
    {
        if (tag is null)
        {
            return null;
        }

        return ValidateTag(tag);
    }

    private static List<string> PrepareTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var uniqueTags = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            var trimmedTag = ValidateTag(tag);
            if (seen.Add(NormalizeTag(trimmedTag)))
            {
                uniqueTags.Add(trimmedTag);
            }
        }

        if (uniqueTags.Count == 0)
        {
            throw new ValidationException("At least one tag is required.");
        }

        return uniqueTags;
    }

    private static string ValidateTag(string? tag)
    {
        if (tag is null)
        {
            throw new ValidationException("Tag is required.");
        }

        var trimmedTag = tag.Trim();
        if (trimmedTag.Length == 0)
        {
            throw new ValidationException("Tag must contain at least one non-whitespace character.");
        }

        if (trimmedTag.Length > AlertConstants.TagMaxLength)
        {
            throw new ValidationException($"Tag must be {AlertConstants.TagMaxLength} characters or fewer.");
        }

        return trimmedTag;
    }

    private static string NormalizeTag(string tag) => tag.ToUpperInvariant();
}
