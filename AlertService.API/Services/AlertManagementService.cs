using AlertService.API.Mappings;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Data.Interfaces;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;
    private readonly AlertService.API.Configurations.AlertDeduplicationOptions _dedupeOptions;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        Microsoft.Extensions.Options.IOptions<AlertService.API.Configurations.AlertDeduplicationOptions> dedupeOptions)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _dedupeOptions = dedupeOptions?.Value ?? new AlertService.API.Configurations.AlertDeduplicationOptions();
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

    public async Task<AlertService.DTO.Responses.CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check for duplicates within the suppression window
        var windowMinutes = Math.Max(0, _dedupeOptions.SuppressionWindowMinutes);
        var since = now.AddMinutes(-windowMinutes);

        var titleNormalized = request.Title?.Trim() ?? string.Empty;
        var existing = await _repository.FindActiveByTitleAndSeveritySinceAsync(titleNormalized, request.Severity, since, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert; returning existing {AlertId}", existing.Id);
            return new AlertService.DTO.Responses.CreateAlertResult
            {
                Alert = existing.ToResponse(),
                Suppressed = true,
                IsNew = false
            };
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new AlertService.DTO.Responses.CreateAlertResult
        {
            Alert = created.ToResponse(),
            Suppressed = false,
            IsNew = true
        };
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

    public async Task<AlertResponse?> AddTagsAsync(int id, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        if (tags is null) throw new ArgumentNullException(nameof(tags));

        var updated = await _repository.AddTagsAsync(id, tags, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        _logger.LogInformation("Added tags to alert {AlertId}", id);
        return updated.ToResponseWithTags();
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));

        var result = await _repository.RemoveTagAsync(id, tag, cancellationToken);
        if (!result)
        {
            _logger.LogWarning("Cannot remove tag '{Tag}' from alert {AlertId}: not found or not assigned", tag, id);
        }

        return result;
    }

    public async Task<AlertTrendsResponse> GetTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        var trends = await _repository.GetTrendsAsync(days, cancellationToken);

        var response = new AlertTrendsResponse();
        response.DailyTrends = trends.Select(t => new DailyAlertTrendResponse
        {
            Date = t.Date,
            SeverityCounts = new AlertSeverityCountsResponse
            {
                Low = t.Low,
                Medium = t.Medium,
                High = t.High,
                Critical = t.Critical
            }
        }).ToList();

        return response;
    }
}
