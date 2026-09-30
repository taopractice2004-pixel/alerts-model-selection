using AlertService.API.Mappings;
using AlertService.Common.Enums;
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
    private readonly ILogger<AlertManagementService> _logger;
    private readonly int _duplicateSuppressionWindowMinutes;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _duplicateSuppressionWindowMinutes = configuration.GetValue<int>("Alerts:DuplicateSuppressionWindowMinutes");
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
            NormalizeFilterTags(request.Tags),
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

    public async Task<IReadOnlyList<AlertTrendResponse>> GetTrendsAsync(AlertTrendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startDate = today.AddDays(-(request.Days - 1));
        var endDate = today.AddDays(1);
        var groupedCounts = await _repository.GetTrendCountsAsync(startDate, endDate, cancellationToken);
        var countsByDateAndSeverity = groupedCounts.ToDictionary(
            item => (item.Date.Date, item.Severity),
            item => item.Count);

        return Enumerable.Range(0, request.Days)
            .Select(offset =>
            {
                var date = startDate.AddDays(offset);
                return new AlertTrendResponse
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Low = GetTrendCount(countsByDateAndSeverity, date, Severity.Low),
                    Medium = GetTrendCount(countsByDateAndSeverity, date, Severity.Medium),
                    High = GetTrendCount(countsByDateAndSeverity, date, Severity.High),
                    Critical = GetTrendCount(countsByDateAndSeverity, date, Severity.Critical)
                };
            })
            .ToList();
    }

    private static int GetTrendCount(
        IReadOnlyDictionary<(DateTime Date, Severity Severity), int> counts,
        DateTime date,
        Severity severity)
    {
        return counts.GetValueOrDefault((date, severity));
    }

    public async Task<(AlertResponse Alert, bool DuplicateSuppressed)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var alert = request.ToEntity(now);
        var duplicate = await _repository.GetActiveNearDuplicateAsync(
            alert.Title,
            alert.Severity,
            now.Subtract(TimeSpan.FromMinutes(_duplicateSuppressionWindowMinutes)),
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert {AlertId}", duplicate.Id);
            return (duplicate.ToResponse(), true);
        }

        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (created.ToResponse(), false);
    }

    public async Task<AlertResponse?> AddTagsAsync(int alertId, AlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tags = NormalizeTags(request.Tags);
        var alert = await _repository.AddTagsAsync(alertId, tags, cancellationToken);
        return alert?.ToResponse();
    }

    public Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTags = NormalizeTags(new[] { tag });
        return _repository.RemoveTagAsync(alertId, normalizedTags.Single(), cancellationToken);
    }

    private static IReadOnlyCollection<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var normalizedTags = (tags ?? throw new ArgumentException("At least one tag is required.", nameof(tags)))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedTags.Length == 0 || normalizedTags.Any(tag => tag.Length is < 1 or > 30))
        {
            throw new ArgumentException("Each tag must be between 1 and 30 characters.", nameof(tags));
        }

        if (normalizedTags.Length > 10)
        {
            throw new ArgumentException("An alert cannot have more than 10 unique tags.", nameof(tags));
        }

        return normalizedTags;
    }

    private static IReadOnlyCollection<string>? NormalizeFilterTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags)
            ? null
            : NormalizeTags(tags.Split(',', StringSplitOptions.None));
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
