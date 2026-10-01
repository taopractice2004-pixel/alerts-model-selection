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

    public async Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var fromInclusive = todayUtc.AddDays(-(request.Days - 1));
        var toExclusive = todayUtc.AddDays(1);

        var dailyCounts = await _repository.GetDailyTrendsAsync(fromInclusive, toExclusive, cancellationToken);

        // Index raw counts by (day, severity) for O(1) lookup while filling buckets.
        var countsByDay = dailyCounts
            .GroupBy(entry => entry.Day)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(entry => entry.Severity, entry => entry.Count));

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var offset = 0; offset < request.Days; offset++)
        {
            var day = fromInclusive.AddDays(offset);
            countsByDay.TryGetValue(day, out var severityCounts);

            var low = GetCount(severityCounts, Severity.Low);
            var medium = GetCount(severityCounts, Severity.Medium);
            var high = GetCount(severityCounts, Severity.High);
            var critical = GetCount(severityCounts, Severity.Critical);

            buckets.Add(new AlertTrendBucketResponse
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

        return new AlertTrendsResponse { Buckets = buckets };
    }

    private static int GetCount(IReadOnlyDictionary<Severity, int>? severityCounts, Severity severity)
    {
        return severityCounts is not null && severityCounts.TryGetValue(severity, out var count) ? count : 0;
    }

    public async Task<AlertCreateResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var createdOnOrAfter = nowUtc - TimeSpan.FromMinutes(_suppressionOptions.WindowMinutes);
        var normalizedTitle = request.Title.Trim();

        var duplicate = await _repository.FindActiveDuplicateAsync(normalizedTitle, request.Severity, createdOnOrAfter, cancellationToken);
        if (duplicate is not null)
        {
            _logger.LogInformation("Suppressed duplicate alert creation; returning existing alert {AlertId}", duplicate.Id);
            return new AlertCreateResult(duplicate.ToResponse(), true);
        }

        var alert = request.ToEntity(nowUtc);
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

    public async Task<AlertResponse?> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return null;
        }

        // Trim, drop empties and deduplicate within the request case-insensitively,
        // keeping the first-seen casing as the canonical representation.
        var requestedTags = request.Tags
            .Select(tag => tag?.Trim() ?? string.Empty)
            .Where(tag => tag.Length > 0)
            .GroupBy(tag => tag.ToLowerInvariant())
            .Select(group => group.First())
            .ToList();

        var existingNames = new HashSet<string>(
            alert.Tags.Select(tag => tag.Name),
            StringComparer.OrdinalIgnoreCase);

        var tagsToAdd = requestedTags
            .Where(tag => !existingNames.Contains(tag))
            .ToList();

        if (existingNames.Count + tagsToAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            throw new AlertValidationException(
                $"An alert cannot have more than {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (tagsToAdd.Count > 0)
        {
            await _repository.AddTagsAsync(id, tagsToAdd, cancellationToken);
            alert = await _repository.GetByIdAsync(id, cancellationToken);

            _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", tagsToAdd.Count, id);
        }

        return alert!.ToResponse();
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var removed = await _repository.RemoveTagAsync(id, tag, cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert or tag assignment not found", id);
            return false;
        }

        _logger.LogInformation("Removed a tag from alert {AlertId}", id);
        return true;
    }
}
