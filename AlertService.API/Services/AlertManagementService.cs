using AlertService.API.Mappings;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Configuration;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private const string DuplicateSuppressionWindowMinutesConfigKey = "Alerts:DuplicateSuppressionWindowMinutes";
    private const int DefaultDuplicateSuppressionWindowMinutes = 15;

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
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var todayUtc = DateOnly.FromDateTime(utcNow);
        var startDate = todayUtc.AddDays(-(days - 1));
        var startUtcInclusive = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtcExclusive = todayUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var counts = await _repository.GetDailySeverityCountsAsync(startUtcInclusive, endUtcExclusive, cancellationToken);
        var countsByDate = counts.ToDictionary(
            count => DateOnly.FromDateTime(count.DateUtc),
            count => count);

        var result = new List<AlertDailyTrendResponse>(days);
        for (var offset = 0; offset < days; offset++)
        {
            var date = startDate.AddDays(offset);
            var hasCounts = countsByDate.TryGetValue(date, out var dayCounts);
            var lowCount = hasCounts ? dayCounts.LowCount : 0;
            var mediumCount = hasCounts ? dayCounts.MediumCount : 0;
            var highCount = hasCounts ? dayCounts.HighCount : 0;
            var criticalCount = hasCounts ? dayCounts.CriticalCount : 0;

            result.Add(new AlertDailyTrendResponse
            {
                Date = date,
                TotalCount = lowCount + mediumCount + highCount + criticalCount,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = lowCount,
                    Medium = mediumCount,
                    High = highCount,
                    Critical = criticalCount
                }
            });
        }

        return result;
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var suppressionWindowMinutes = GetDuplicateSuppressionWindowMinutes();
        var createdFromUtc = utcNow.AddMinutes(-suppressionWindowMinutes);
        var duplicate = await _repository.FindRecentActiveDuplicateAsync(
            request.Title,
            request.Severity,
            createdFromUtc,
            utcNow,
            cancellationToken);

        if (duplicate is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title {Title} and severity {Severity} within {WindowMinutes} minute window; using existing alert {AlertId}",
                request.Title,
                request.Severity,
                suppressionWindowMinutes,
                duplicate.Id);
            return new CreateAlertResult(duplicate.ToResponse(), true);
        }

        var alert = request.ToEntity(utcNow);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(created.ToResponse(), false);
    }

    public async Task<AlertResponse?> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validatedTags = ValidateAndDistinctTags(request.Tags);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        var existingNormalizedTags = alert.Tags
            .Select(tag => NormalizeTag(tag.Value))
            .ToHashSet(StringComparer.Ordinal);
        var newTagCount = validatedTags.Count(tag => !existingNormalizedTags.Contains(NormalizeTag(tag)));

        if (alert.Tags.Count + newTagCount > 10)
        {
            throw new ArgumentException("An alert cannot have more than 10 tags.", nameof(request.Tags));
        }

        var updatedAlert = await _repository.AddTagsAsync(id, validatedTags, cancellationToken);
        return updatedAlert?.ToResponse();
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

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty or whitespace.", nameof(tag));
        }

        if (tag.Trim().Length is < 1 or > 30)
        {
            throw new ArgumentException("Tag must be between 1 and 30 characters.", nameof(tag));
        }

        var removed = await _repository.RemoveTagAsync(id, tag, cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert or assignment not found", id);
        }

        return removed;
    }

    private static List<string> ValidateAndDistinctTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var distinctTags = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawTag in tags)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                throw new ArgumentException("Tags cannot contain empty or whitespace values.", nameof(tags));
            }

            var trimmed = rawTag.Trim();
            if (trimmed.Length is < 1 or > 30)
            {
                throw new ArgumentException("Each tag must be between 1 and 30 characters.", nameof(tags));
            }

            var normalized = NormalizeTag(trimmed);
            if (seen.Add(normalized))
            {
                distinctTags.Add(trimmed);
            }
        }

        return distinctTags;
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }

    private int GetDuplicateSuppressionWindowMinutes()
    {
        var configuredMinutes = _configuration.GetValue<int?>(DuplicateSuppressionWindowMinutesConfigKey);
        if (configuredMinutes.GetValueOrDefault() > 0)
        {
            return configuredMinutes.Value;
        }

        _logger.LogWarning(
            "Configuration value {ConfigKey} is missing or invalid. Falling back to default of {DefaultWindowMinutes} minutes.",
            DuplicateSuppressionWindowMinutesConfigKey,
            DefaultDuplicateSuppressionWindowMinutes);

        return DefaultDuplicateSuppressionWindowMinutes;
    }
}
