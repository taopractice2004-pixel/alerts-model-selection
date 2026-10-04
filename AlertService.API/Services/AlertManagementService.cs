using AlertService.API.Configuration;
using AlertService.API.Mappings;
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
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly AlertSuppressionOptions _suppressionOptions;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<AlertSuppressionOptions> suppressionOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _suppressionOptions = suppressionOptions.Value;
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

    public async Task<AlertTrendResponse> GetTrendsAsync(AlertTrendQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todayUtc = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var startDay = todayUtc.AddDays(-(request.Days - 1));
        var fromUtcInclusive = startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtcExclusive = todayUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var dailyCounts = await _repository.GetDailySeverityCountsAsync(fromUtcInclusive, toUtcExclusive, cancellationToken);
        var countsByDay = dailyCounts.ToLookup(c => DateOnly.FromDateTime(c.DayUtc));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var offset = 0; offset < request.Days; offset++)
        {
            var day = startDay.AddDays(offset);
            var dayCounts = countsByDay[day];

            var severityCounts = new AlertSeverityCountsResponse
            {
                Low = dayCounts.Where(c => c.Severity == Severity.Low).Sum(c => c.Count),
                Medium = dayCounts.Where(c => c.Severity == Severity.Medium).Sum(c => c.Count),
                High = dayCounts.Where(c => c.Severity == Severity.High).Sum(c => c.Count),
                Critical = dayCounts.Where(c => c.Severity == Severity.Critical).Sum(c => c.Count)
            };

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = day,
                TotalCount = severityCounts.Low + severityCounts.Medium + severityCounts.High + severityCounts.Critical,
                SeverityCounts = severityCounts
            });
        }

        return new AlertTrendResponse
        {
            Days = request.Days,
            Buckets = buckets
        };
    }

    public async Task<(AlertCreationStatus Status, AlertResponse Alert)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStartUtc = nowUtc.AddMinutes(-_suppressionOptions.WindowMinutes);

        var duplicate = await _repository.FindActiveDuplicateAsync(request.Title.Trim(), request.Severity, windowStartUtc, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert for severity {Severity}; returning existing alert {AlertId}", duplicate.Severity, duplicate.Id);
            return (AlertCreationStatus.DuplicateSuppressed, duplicate.ToResponse());
        }

        var alert = request.ToEntity(nowUtc);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (AlertCreationStatus.Created, created.ToResponse());
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

    public async Task<(TagOperationStatus Status, AlertResponse? Alert)> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return (TagOperationStatus.AlertNotFound, null);
        }

        // Trim, drop empties, and dedupe the incoming tags case-insensitively.
        var incoming = request.Tags
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => t.Length > 0)
            .GroupBy(t => t.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        var existingNames = new HashSet<string>(alert.Tags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var tagsToAdd = incoming.Where(t => !existingNames.Contains(t)).ToList();

        if (alert.Tags.Count + tagsToAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: would exceed {Max} tags", id, AlertConstants.MaxTagsPerAlert);
            return (TagOperationStatus.TagLimitExceeded, null);
        }

        if (tagsToAdd.Count > 0)
        {
            await _repository.AddTagsToAlertAsync(alert, tagsToAdd, cancellationToken);
            _logger.LogInformation("Added {Count} tag(s) to alert {AlertId}", tagsToAdd.Count, id);
        }

        return (TagOperationStatus.Success, alert.ToResponse());
    }

    public async Task<TagOperationStatus> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return TagOperationStatus.AlertNotFound;
        }

        var normalized = tag?.Trim() ?? string.Empty;
        var assigned = alert.Tags.FirstOrDefault(t => string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: tag not assigned", id);
            return TagOperationStatus.TagNotAssigned;
        }

        await _repository.RemoveTagFromAlertAsync(alert, assigned, cancellationToken);
        _logger.LogInformation("Removed tag from alert {AlertId}", id);
        return TagOperationStatus.Success;
    }
}
