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
    private readonly DuplicateSuppressionOptions _suppressionOptions;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<DuplicateSuppressionOptions> suppressionOptions,
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

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var fromInclusive = today.AddDays(-(request.Days - 1));
        var toExclusive = today.AddDays(1);

        var counts = await _repository.GetDailyCountsBySeverityAsync(fromInclusive, toExclusive, cancellationToken);

        var countsByDay = counts
            .GroupBy(row => row.Date)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(row => row.Severity, row => row.Count));

        var days = new List<AlertTrendDayResponse>(request.Days);
        for (var offset = 0; offset < request.Days; offset++)
        {
            var date = fromInclusive.AddDays(offset);
            countsByDay.TryGetValue(date, out var severityCounts);
            severityCounts ??= new Dictionary<Severity, int>();

            var low = severityCounts.GetValueOrDefault(Severity.Low);
            var medium = severityCounts.GetValueOrDefault(Severity.Medium);
            var high = severityCounts.GetValueOrDefault(Severity.High);
            var critical = severityCounts.GetValueOrDefault(Severity.Critical);

            days.Add(new AlertTrendDayResponse
            {
                Date = DateOnly.FromDateTime(date),
                TotalCount = low + medium + high + critical,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = low,
                    Medium = medium,
                    High = high,
                    Critical = critical
                }
            });
        }

        return new AlertTrendResponse { Days = days };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var threshold = now.AddMinutes(-_suppressionOptions.WindowMinutes);

        var duplicate = await _repository.FindActiveDuplicateAsync(request.Title, request.Severity, threshold, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed near-duplicate alert (severity {Severity}); returning existing alert {AlertId}",
                duplicate.Severity, duplicate.Id);
            return new CreateAlertResult(true, duplicate.ToResponse());
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(false, created.ToResponse());
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

    public async Task<AddTagsResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return new AddTagsResult(AddTagsStatus.AlertNotFound, null);
        }

        var existingNames = new HashSet<string>(
            alert.AlertTags.Select(at => at.Tag.Name),
            StringComparer.OrdinalIgnoreCase);

        var newNames = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in request.Tags)
        {
            var name = raw.Trim();
            if (name.Length == 0 || existingNames.Contains(name) || !seen.Add(name))
            {
                continue;
            }

            newNames.Add(name);
        }

        if (existingNames.Count + newNames.Count > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Rejected tag add for alert {AlertId}: would exceed {Max} tags", id, AlertConstants.MaxTagsPerAlert);
            return new AddTagsResult(AddTagsStatus.TagLimitExceeded, null);
        }

        if (newNames.Count > 0)
        {
            await _repository.AddTagsAsync(alert, newNames, cancellationToken);
            _logger.LogInformation("Added {Count} tag(s) to alert {AlertId}", newNames.Count, id);
        }

        return new AddTagsResult(AddTagsStatus.Success, alert.ToResponse());
    }

    public async Task<RemoveTagStatus> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return RemoveTagStatus.AlertNotFound;
        }

        var removed = await _repository.RemoveTagAsync(alert, tag.Trim(), cancellationToken);
        if (!removed)
        {
            return RemoveTagStatus.TagNotFound;
        }

        _logger.LogInformation("Removed tag from alert {AlertId}", id);
        return RemoveTagStatus.Removed;
    }
}
