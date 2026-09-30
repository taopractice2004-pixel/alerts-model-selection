using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using System.ComponentModel.DataAnnotations;

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
        int duplicateSuppressionWindowMinutes)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _duplicateSuppressionWindowMinutes = Math.Max(0, duplicateSuppressionWindowMinutes);
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

    public async Task<AlertTrendResponse> GetTrendsAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be between 1 and 90.");
        }

        var utcToday = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var startDay = utcToday.AddDays(-(days - 1));
        var endExclusive = utcToday.AddDays(1);

        var counts = await _repository.GetDailySeverityCountsAsync(
            startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            cancellationToken);

        var countsByDay = counts
            .GroupBy(c => DateOnly.FromDateTime(c.Day))
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.Severity, item => item.Count));

        var response = new AlertTrendResponse();
        for (var offset = 0; offset < days; offset++)
        {
            var day = startDay.AddDays(offset);
            countsByDay.TryGetValue(day, out var perSeverity);

            var low = perSeverity?.GetValueOrDefault(Common.Enums.Severity.Low) ?? 0;
            var medium = perSeverity?.GetValueOrDefault(Common.Enums.Severity.Medium) ?? 0;
            var high = perSeverity?.GetValueOrDefault(Common.Enums.Severity.High) ?? 0;
            var critical = perSeverity?.GetValueOrDefault(Common.Enums.Severity.Critical) ?? 0;

            response.Days.Add(new AlertTrendDayResponse
            {
                Date = day,
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

        return response;
    }

    public async Task<(AlertResponse Alert, bool IsDuplicateSuppressed)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTitle = request.Title.Trim();
        var createdTo = _timeProvider.GetUtcNow().UtcDateTime;
        var createdFrom = createdTo.AddMinutes(-_duplicateSuppressionWindowMinutes);

        var (candidates, _) = await _repository.GetAllAsync(
            isActive: true,
            severity: request.Severity,
            createdFrom: createdFrom,
            createdTo: createdTo,
            search: normalizedTitle,
            sortBy: AlertConstants.SortByCreatedDate,
            sortDirection: AlertConstants.SortDirectionDesc,
            page: 1,
            pageSize: 100,
            cancellationToken: cancellationToken);

        var existing = candidates.FirstOrDefault(alert =>
            string.Equals(alert.Title, normalizedTitle, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert creation for title '{Title}' with severity {Severity}; matched existing alert {ExistingAlertId}",
                normalizedTitle,
                request.Severity,
                existing.Id);

            return (existing.ToResponse(), true);
        }

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (created.ToResponse(), false);
    }

    public async Task<AlertResponse?> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTags = request.Tags
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var current = await _repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var existingTags = current.AlertTags
            .Select(at => at.Tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = normalizedTags.Where(tag => !existingTags.Contains(tag)).ToArray();
        if (existingTags.Count + toAdd.Length > AlertConstants.MaxTagsPerAlert)
        {
            throw new ValidationException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (toAdd.Length == 0)
        {
            return current.ToResponse();
        }

        var updated = await _repository.AddTagsAsync(id, toAdd, cancellationToken);
        return updated?.ToResponse();
    }

    public Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Task.FromResult(false);
        }

        return _repository.RemoveTagAsync(id, tag.Trim(), cancellationToken);
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
}
