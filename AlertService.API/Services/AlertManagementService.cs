using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;
using Microsoft.Extensions.Options;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly DuplicateSuppressionOptions _duplicateSuppression;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        IOptions<DuplicateSuppressionOptions> duplicateSuppression,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _duplicateSuppression = duplicateSuppression.Value;
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

    public async Task<IReadOnlyList<AlertTrendBucketResponse>> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var firstDay = today.AddDays(-(request.Days - 1));

        var counts = await _repository.GetDailyCountsAsync(firstDay, today.AddDays(1), cancellationToken);
        var countsByDay = counts
            .GroupBy(c => c.Date.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var day = firstDay; day <= today; day = day.AddDays(1))
        {
            var dayCounts = countsByDay.GetValueOrDefault(day) ?? [];
            var severityCounts = new AlertSeverityCountsResponse
            {
                Low = dayCounts.Where(c => c.Severity == Severity.Low).Sum(c => c.Count),
                Medium = dayCounts.Where(c => c.Severity == Severity.Medium).Sum(c => c.Count),
                High = dayCounts.Where(c => c.Severity == Severity.High).Sum(c => c.Count),
                Critical = dayCounts.Where(c => c.Severity == Severity.Critical).Sum(c => c.Count)
            };

            buckets.Add(new AlertTrendBucketResponse
            {
                Date = DateOnly.FromDateTime(day),
                TotalCount = severityCounts.Low + severityCounts.Medium + severityCounts.High + severityCounts.Critical,
                SeverityCounts = severityCounts
            });
        }

        return buckets;
    }

    public async Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (_duplicateSuppression.WindowMinutes > 0)
        {
            var windowStart = now - TimeSpan.FromMinutes(_duplicateSuppression.WindowMinutes);
            var duplicate = await _repository.FindRecentActiveDuplicateAsync(
                request.Title.Trim(),
                request.Severity,
                windowStart,
                cancellationToken);

            if (duplicate is not null)
            {
                _logger.LogInformation("Suppressed duplicate alert; returning existing alert {AlertId}", duplicate.Id);
                return new CreateAlertResult(duplicate.ToResponse(), true);
            }
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(created.ToResponse(), false);
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

        var currentNames = alert.Tags.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var newNames = request.Tags
            .Select(Tag.NormalizeName)
            .Where(name => !currentNames.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (newNames.Count == 0)
        {
            return new AddAlertTagsResult(AddAlertTagsStatus.Added, alert.ToResponse());
        }

        if (currentNames.Count + newNames.Count > AlertConstants.MaxTagsPerAlert)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: limit of {MaxTags} tags exceeded", id, AlertConstants.MaxTagsPerAlert);
            return new AddAlertTagsResult(AddAlertTagsStatus.TagLimitExceeded, null);
        }

        var existingTags = (await _repository.GetTagsByNamesAsync(newNames, cancellationToken))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        foreach (var name in newNames)
        {
            alert.Tags.Add(existingTags.TryGetValue(name, out var existing) ? existing : new Tag { Name = name });
        }

        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Added {TagCount} tags to alert {AlertId}", newNames.Count, id);
        return new AddAlertTagsResult(AddAlertTagsStatus.Added, alert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        var normalized = Tag.NormalizeName(tag);
        var assigned = alert.Tags.FirstOrDefault(t => t.Name == normalized);
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: not assigned", normalized, id);
            return false;
        }

        alert.Tags.Remove(assigned);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", normalized, id);
        return true;
    }
}
