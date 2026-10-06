using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Configuration;

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

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger)
        : this(repository, timeProvider, new ConfigurationManager(), logger)
    {
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
            request.SortBy,
            request.SortDirection,
            request.Page,
            request.PageSize,
            request.Tag,
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

    public async Task<IReadOnlyList<AlertTrendResponse>> GetTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        var trends = await _repository.GetTrendsAsync(days, cancellationToken);

        return trends
            .Select(trend => new AlertTrendResponse
            {
                Date = trend.DateUtc,
                TotalCount = trend.TotalCount,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = trend.LowCount,
                    Medium = trend.MediumCount,
                    High = trend.HighCount,
                    Critical = trend.CriticalCount
                }
            })
            .ToList();
    }

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var duplicateWindowMinutes = _configuration.GetValue<int?>("AlertSuppression:DuplicateWindowMinutes");
        if (duplicateWindowMinutes is null or <= 0)
        {
            throw new InvalidOperationException("AlertSuppression:DuplicateWindowMinutes must be configured as a positive integer.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var createdAfterUtc = nowUtc.AddMinutes(-duplicateWindowMinutes.Value);
        var duplicate = await _repository.GetActiveDuplicateByTitleAndSeverityAsync(
            request.Title,
            request.Severity,
            createdAfterUtc,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert creation. Existing alert {AlertId} matched title and severity within {WindowMinutes} minutes.",
                duplicate.Id,
                duplicateWindowMinutes.Value);

            var duplicateResponse = duplicate.ToResponse();
            duplicateResponse.IsDuplicateSuppressed = true;
            return duplicateResponse;
        }

        var alert = request.ToEntity(nowUtc);
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

    public async Task<(AlertResponse? Alert, string? ValidationError)> AddTagsAsync(int id, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags for alert {AlertId}: not found", id);
            return (null, null);
        }

        if (tags.Count == 0)
        {
            return (null, "At least one tag is required.");
        }

        var normalizedTags = new List<string>();
        var seenTagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            if (tag is null)
            {
                return (null, "Tags cannot contain null values.");
            }

            var trimmedTag = tag.Trim();
            if (trimmedTag.Length is < 1 or > AlertConstants.TagMaxLength)
            {
                return (null, $"Each tag must be between 1 and {AlertConstants.TagMaxLength} characters.");
            }

            if (seenTagNames.Add(trimmedTag))
            {
                normalizedTags.Add(trimmedTag);
            }
        }

        if (normalizedTags.Count == 0)
        {
            return (null, "At least one non-empty tag is required.");
        }

        var existingTagNames = alert.AlertTags
            .Select(alertTag => alertTag.Tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tagsToAdd = normalizedTags
            .Where(tag => !existingTagNames.Contains(tag))
            .ToList();

        if (alert.AlertTags.Count + tagsToAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            return (null, $"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (tagsToAdd.Count == 0)
        {
            return (alert.ToResponse(), null);
        }

        var updatedAlert = await _repository.AddTagsAsync(id, tagsToAdd, cancellationToken);
        if (updatedAlert is null)
        {
            _logger.LogWarning("Cannot add tags for alert {AlertId}: not found during persistence", id);
            return (null, null);
        }

        _logger.LogInformation("Added {TagCount} tags for alert {AlertId}", tagsToAdd.Count, id);
        return (updatedAlert.ToResponse(), null);
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var removed = await _repository.RemoveTagAsync(id, tag, cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag '{TagName}' from alert {AlertId}: alert or assignment not found", tag, id);
            return false;
        }

        _logger.LogInformation("Removed tag '{TagName}' from alert {AlertId}", tag, id);
        return true;
    }
}
