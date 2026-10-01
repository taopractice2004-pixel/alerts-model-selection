using AlertService.API.Mappings;
using AlertService.API.Options;
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
    private readonly IOptions<AlertSuppressionOptions> _suppressionOptions;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<AlertSuppressionOptions> suppressionOptions,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _suppressionOptions = suppressionOptions;
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

    public async Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var fromDateUtc = today.AddDays(-(request.Days - 1));

        var rawCounts = await _repository.GetTrendsAsync(fromDateUtc, today, cancellationToken);
        var countsByDate = rawCounts.ToLookup(r => r.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var date = fromDateUtc; date <= today; date = date.AddDays(1))
        {
            var dayCounts = countsByDate[date].ToList();

            var severityCounts = new AlertSeverityCountsResponse
            {
                Low = dayCounts.Where(c => c.Severity == Severity.Low).Sum(c => c.Count),
                Medium = dayCounts.Where(c => c.Severity == Severity.Medium).Sum(c => c.Count),
                High = dayCounts.Where(c => c.Severity == Severity.High).Sum(c => c.Count),
                Critical = dayCounts.Where(c => c.Severity == Severity.Critical).Sum(c => c.Count)
            };

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = date,
                TotalCount = severityCounts.Low + severityCounts.Medium + severityCounts.High + severityCounts.Critical,
                SeverityCounts = severityCounts
            });
        }

        return new AlertTrendsResponse { Buckets = buckets };
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - TimeSpan.FromMinutes(_suppressionOptions.Value.DuplicateWindowMinutes);
        var duplicate = await _repository.FindActiveDuplicateAsync(request.Title.Trim(), request.Severity, cutoff, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert with title '{Title}' and severity {Severity}; matched existing alert {AlertId}",
                request.Title,
                request.Severity,
                duplicate.Id);
            return new CreateAlertResult(CreateAlertStatus.DuplicateSuppressed, duplicate.ToResponse());
        }

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(CreateAlertStatus.Created, created.ToResponse());
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

    public async Task<AddAlertTagsResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return new AddAlertTagsResult(AddAlertTagsStatus.AlertNotFound, null);
        }

        // Normalize and dedupe the incoming tags case-insensitively (trim + distinct-by-ordinal-ignore-case).
        var normalizedNewTags = new List<string>();
        foreach (var rawTag in request.Tags)
        {
            var trimmed = rawTag.Trim();
            if (!normalizedNewTags.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                normalizedNewTags.Add(trimmed);
            }
        }

        var existingNames = alert.Tags.Select(at => at.Tag.Name).ToList();
        var distinctTagNamesAfterAdd = existingNames
            .Concat(normalizedNewTags)
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Count();

        if (distinctTagNamesAfterAdd > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: would exceed {MaxTags} tags", id, AlertConstants.MaxTagsPerAlert);
            return new AddAlertTagsResult(AddAlertTagsStatus.TooManyTags, null);
        }

        var updated = await _repository.AddTagsAsync(id, normalizedNewTags, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return new AddAlertTagsResult(AddAlertTagsStatus.AlertNotFound, null);
        }

        _logger.LogInformation("Added tags to alert {AlertId}", id);
        return new AddAlertTagsResult(AddAlertTagsStatus.Success, updated.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var removed = await _repository.RemoveTagAsync(id, tag, cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag '{Tag}' from alert {AlertId}: alert or tag assignment not found", tag, id);
        }
        else
        {
            _logger.LogInformation("Removed tag '{Tag}' from alert {AlertId}", tag, id);
        }

        return removed;
    }
}
