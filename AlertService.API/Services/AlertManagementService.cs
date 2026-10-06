using AlertService.API.Configuration;
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

    public async Task<IReadOnlyList<AlertTrendBucketResponse>> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var startDate = today.AddDays(-(request.Days - 1));

        var counts = await _repository.GetCreatedCountsByDayAsync(startDate, cancellationToken);
        var countsByDay = counts.ToLookup(c => c.Date.Date);

        var buckets = new List<AlertTrendBucketResponse>(request.Days);
        for (var day = startDate; day <= today; day = day.AddDays(1))
        {
            var dayCounts = countsByDay[day].ToList();
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

        if (_suppressionOptions.DuplicateWindowMinutes > 0)
        {
            var windowStart = now.AddMinutes(-_suppressionOptions.DuplicateWindowMinutes);
            var duplicate = await _repository.FindRecentActiveDuplicateAsync(
                request.Title.Trim(), request.Severity, windowStart, cancellationToken);

            if (duplicate is not null)
            {
                _logger.LogInformation("Suppressed duplicate alert; returning existing alert {AlertId}", duplicate.Id);
                return new CreateAlertResult(duplicate.ToResponse(), DuplicateSuppressed: true);
            }
        }

        var alert = request.ToEntity(now);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return new CreateAlertResult(created.ToResponse(), DuplicateSuppressed: false);
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
            return AddAlertTagsResult.NotFound();
        }

        // Keyed by normalized name so duplicates within the request collapse, keeping first-seen casing.
        var requested = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawTag in request.Tags ?? new List<string>())
        {
            var name = rawTag?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length < AlertConstants.TagMinLength || name.Length > AlertConstants.TagMaxLength)
            {
                return AddAlertTagsResult.Invalid(
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.");
            }

            requested.TryAdd(Tag.Normalize(name), name);
        }

        if (requested.Count == 0)
        {
            return AddAlertTagsResult.Invalid("At least one tag is required.");
        }

        var existing = alert.Tags.Select(t => t.NormalizedName).ToHashSet(StringComparer.Ordinal);
        var toAdd = requested.Where(pair => !existing.Contains(pair.Key)).Select(pair => pair.Value).ToList();

        if (alert.Tags.Count + toAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            return AddAlertTagsResult.Invalid($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        if (toAdd.Count > 0)
        {
            await _repository.AddTagsAsync(alert, toAdd, cancellationToken);
            _logger.LogInformation("Added {TagCount} tag(s) to alert {AlertId}", toAdd.Count, id);
        }

        return AddAlertTagsResult.Success(alert.ToResponse());
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(tag) || tag.Trim().Length > AlertConstants.TagMaxLength)
        {
            return false;
        }

        var normalized = Tag.Normalize(tag);
        var assigned = alert.Tags.FirstOrDefault(t => t.NormalizedName == normalized);
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: tag not assigned", id);
            return false;
        }

        await _repository.RemoveTagAsync(alert, assigned, cancellationToken);

        _logger.LogInformation("Removed tag from alert {AlertId}", id);
        return true;
    }
}
