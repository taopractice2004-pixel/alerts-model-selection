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
    private readonly int _duplicateSuppressionWindowMinutes;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        IOptions<AlertSuppressionOptions>? alertSuppressionOptions = null)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;

        _duplicateSuppressionWindowMinutes = alertSuppressionOptions?.Value.DuplicateSuppressionWindowMinutes
            ?? AlertSuppressionOptions.DefaultDuplicateSuppressionWindowMinutes;

        if (_duplicateSuppressionWindowMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alertSuppressionOptions),
                "Alerts:DuplicateSuppressionWindowMinutes must be greater than zero.");
        }
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

    public async Task<IReadOnlyList<AlertDailyTrendResponse>> GetDailyTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startDateUtc = todayUtc.AddDays(-(days - 1));
        var endDateUtcExclusive = todayUtc.AddDays(1);

        var aggregatedDays = await _repository.GetDailyTrendCountsAsync(
            startDateUtc,
            endDateUtcExclusive,
            cancellationToken);

        var dayLookup = aggregatedDays.ToDictionary(
            result => result.DayUtc.Date,
            result => result);

        var trends = new List<AlertDailyTrendResponse>(days);

        for (var offset = 0; offset < days; offset++)
        {
            var day = startDateUtc.AddDays(offset);

            if (dayLookup.TryGetValue(day, out var countResult))
            {
                trends.Add(new AlertDailyTrendResponse
                {
                    DayUtc = day,
                    TotalCount = countResult.TotalCount,
                    SeverityCounts = new AlertSeverityCountsResponse
                    {
                        Low = countResult.LowCount,
                        Medium = countResult.MediumCount,
                        High = countResult.HighCount,
                        Critical = countResult.CriticalCount
                    }
                });

                continue;
            }

            trends.Add(new AlertDailyTrendResponse
            {
                DayUtc = day,
                TotalCount = 0,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = 0,
                    Medium = 0,
                    High = 0,
                    Critical = 0
                }
            });
        }

        return trends;
    }

    public async Task<AlertCreateResult> CreateWithSuppressionAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var suppressionWindowStart = now.AddMinutes(-_duplicateSuppressionWindowMinutes);
        var normalizedTitle = request.Title.Trim();

        var duplicate = await _repository.FindActiveDuplicateAsync(
            normalizedTitle,
            request.Severity,
            suppressionWindowStart,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title '{Title}' and severity {Severity}; returning alert {AlertId}",
                normalizedTitle,
                request.Severity,
                duplicate.Id);

            return new AlertCreateResult
            {
                Alert = duplicate.ToResponse(),
                DuplicateSuppressed = true
            };
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);

        return new AlertCreateResult
        {
            Alert = created.ToResponse(),
            DuplicateSuppressed = false
        };
    }

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        var result = await CreateWithSuppressionAsync(request, cancellationToken);
        return result.Alert;
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

    public async Task<TagAssignmentResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return TagAssignmentResult.AlertNotFound();
        }

        var normalizedTags = request.Tags
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newTags = normalizedTags
            .Where(tag => !existingTags.Contains(tag))
            .ToList();

        if (existingTags.Count + newTags.Count > AlertConstants.MaxTagsPerAlert)
        {
            return TagAssignmentResult.MaxTagsExceeded();
        }

        if (newTags.Count == 0)
        {
            return TagAssignmentResult.Success(alert.ToResponse());
        }

        var updatedAlert = await _repository.AddTagsAsync(id, newTags, cancellationToken);
        if (updatedAlert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return TagAssignmentResult.AlertNotFound();
        }

        _logger.LogInformation("Added {TagCount} tags to alert {AlertId}", newTags.Count, id);
        return TagAssignmentResult.Success(updatedAlert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = tag.Trim();
        if (normalizedTag.Length == 0)
        {
            return false;
        }

        var removed = await _repository.RemoveTagAsync(id, normalizedTag, cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag '{Tag}' from alert {AlertId}: alert or assignment not found", normalizedTag, id);
            return false;
        }

        _logger.LogInformation("Removed tag '{Tag}' from alert {AlertId}", normalizedTag, id);
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
}
