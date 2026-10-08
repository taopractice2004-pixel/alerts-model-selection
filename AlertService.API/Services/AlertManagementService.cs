using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PagedResponse<AlertResponse>> GetAllAsync(AlertQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new AlertQueryOptions
        {
            IsActive = request.IsActive,
            Severity = request.Severity,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            Search = request.Search,
            Tag = request.Tag,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (alerts, totalCount) = await _repository.GetAllAsync(options, cancellationToken);

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

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return created.ToResponse();
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

    public async Task<AlertResponse?> AddTagsAsync(int id, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var normalizedTags = NormalizeAndValidateTags(tags);
        var existingAlert = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingAlert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var finalTagCount = existingAlert.AlertTags
            .Select(at => at.Tag.NormalizedValue)
            .Union(normalizedTags, StringComparer.Ordinal)
            .Count();

        if (finalTagCount > AlertConstants.MaxTagsPerAlert)
        {
            throw new ArgumentException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.", nameof(tags));
        }

        var updated = await _repository.AddTagsAsync(id, normalizedTags, cancellationToken);
        return updated?.ToResponse();
    }

    public Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag is required.", nameof(tag));
        }

        if (tag.Length > AlertConstants.TagMaxLength)
        {
            throw new ArgumentException($"Tag cannot exceed {AlertConstants.TagMaxLength} characters.", nameof(tag));
        }

        return _repository.RemoveTagAsync(id, NormalizeTag(tag), cancellationToken);
    }

    private static IReadOnlyCollection<string> NormalizeAndValidateTags(IReadOnlyCollection<string> tags)
    {
        if (tags.Count == 0)
        {
            throw new ArgumentException("At least one tag is required.", nameof(tags));
        }

        var normalizedTags = tags
            .Select(NormalizeTag)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedTags.Length == 0)
        {
            throw new ArgumentException("At least one valid tag is required.", nameof(tags));
        }

        foreach (var normalizedTag in normalizedTags)
        {
            if (normalizedTag.Length < AlertConstants.TagMinLength || normalizedTag.Length > AlertConstants.TagMaxLength)
            {
                throw new ArgumentException(
                    $"Each tag length must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.",
                    nameof(tags));
            }
        }

        return normalizedTags;
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToLowerInvariant();
    }
}
