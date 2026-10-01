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
    private const string DuplicateSuppressionWindowMinutesConfigKey = "Alerts:DuplicateSuppressionWindowMinutes";
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
            SeverityCounts = CreateSeverityCounts(summary.LowCount, summary.MediumCount, summary.HighCount, summary.CriticalCount)
        };
    }

    public async Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startDateUtc = todayUtc.AddDays(1 - request.Days);
        var endDateUtcExclusive = todayUtc.AddDays(1);
        var trendCounts = await _repository.GetDailyTrendsAsync(startDateUtc, endDateUtcExclusive, cancellationToken);
        var trendCountsByDate = trendCounts.ToDictionary(trendCount => trendCount.Date.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);

        for (var date = startDateUtc; date < endDateUtcExclusive; date = date.AddDays(1))
        {
            if (trendCountsByDate.TryGetValue(date, out var trendCount))
            {
                buckets.Add(new AlertTrendBucketResponse
                {
                    Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                    TotalCount = trendCount.TotalCount,
                    SeverityCounts = CreateSeverityCounts(
                        trendCount.LowCount,
                        trendCount.MediumCount,
                        trendCount.HighCount,
                        trendCount.CriticalCount)
                });

                continue;
            }

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                TotalCount = 0,
                SeverityCounts = CreateSeverityCounts(0, 0, 0, 0)
            });
        }

        return new AlertTrendsResponse
        {
            Days = request.Days,
            Buckets = buckets
        };
    }

    public async Task<AlertCreateResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var alert = request.ToEntity(now);
        var duplicateSuppressionWindowMinutes = _configuration.GetValue<int?>(DuplicateSuppressionWindowMinutesConfigKey);

        if (duplicateSuppressionWindowMinutes is > 0)
        {
            var createdAfterUtc = now.AddMinutes(-duplicateSuppressionWindowMinutes.Value);
            var existing = await _repository.GetActiveDuplicateAsync(alert.Title, alert.Severity, createdAfterUtc, cancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "Suppressed duplicate alert creation for existing alert {AlertId} with severity {Severity}",
                    existing.Id,
                    existing.Severity);

                return new AlertCreateResult(existing.ToResponse(), true);
            }
        }

        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new AlertCreateResult(created.ToResponse(), false);
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

    public async Task<AlertTagOperationResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return AlertTagOperationResult.NotFound();
        }

        var normalizedTags = NormalizeTags(request.Tags);
        if (normalizedTags.Count == 0)
        {
            return AlertTagOperationResult.ValidationFailed("At least one valid tag is required.");
        }

        var existingTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var totalTagCount = existingTags
            .Union(normalizedTags, StringComparer.OrdinalIgnoreCase)
            .Count();

        if (totalTagCount > AlertConstants.MaxTagsPerAlert)
        {
            return AlertTagOperationResult.ValidationFailed(
                $"An alert cannot have more than {AlertConstants.MaxTagsPerAlert} tags.");
        }

        await _repository.AddTagsAsync(alert, normalizedTags, cancellationToken);

        _logger.LogInformation("Added tags to alert {AlertId}", id);
        return AlertTagOperationResult.Success(alert.ToResponse());
    }

    public async Task<AlertTagOperationResult> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return AlertTagOperationResult.NotFound();
        }

        var normalizedTag = tag.Trim();
        var hasTag = alert.AlertTags.Any(alertTag => string.Equals(alertTag.Tag.Name, normalizedTag, StringComparison.OrdinalIgnoreCase));
        if (!hasTag)
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: assignment not found", normalizedTag, id);
            return AlertTagOperationResult.NotFound();
        }

        await _repository.RemoveTagAsync(alert, normalizedTag, cancellationToken);

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", normalizedTag, id);
        return AlertTagOperationResult.Success(alert.ToResponse());
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

    private static List<string> NormalizeTags(IEnumerable<string> tags)
    {
        var normalizedTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var trimmedTag = tag.Trim();
            if (seenTags.Add(trimmedTag))
            {
                normalizedTags.Add(trimmedTag);
            }
        }

        return normalizedTags;
    }

    private static AlertSeverityCountsResponse CreateSeverityCounts(int lowCount, int mediumCount, int highCount, int criticalCount) => new()
    {
        Low = lowCount,
        Medium = mediumCount,
        High = highCount,
        Critical = criticalCount
    };
}
