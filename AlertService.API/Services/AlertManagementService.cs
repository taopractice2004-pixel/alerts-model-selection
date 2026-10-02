using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;
using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
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

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return created.ToResponse();
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

        var requestedNames = NormalizeTagNames(request.Tags);
        var existingNames = alert.Tags
            .Select(t => t.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var namesToAdd = requestedNames
            .Where(n => !existingNames.Contains(n))
            .ToList();

        if (namesToAdd.Count == 0)
        {
            return alert.ToResponse();
        }

        if (alert.Tags.Count + namesToAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            throw new ValidationException($"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags.");
        }

        var existingTags = await _repository.GetTagsByNamesAsync(namesToAdd, cancellationToken);
        var existingTagsByName = existingTags
            .ToDictionary(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var name in namesToAdd)
        {
            var tag = existingTagsByName.TryGetValue(name, out var found)
                ? found
                : new Tag { Name = name };
            alert.Tags.Add(tag);
        }

        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Added {Count} tag(s) to alert {AlertId}", namesToAdd.Count, id);
        return alert.ToResponse();
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: not found", id);
            return false;
        }

        var normalizedTag = (tag ?? string.Empty).Trim();
        var assigned = alert.Tags.FirstOrDefault(t => string.Equals(t.Name, normalizedTag, StringComparison.OrdinalIgnoreCase));
        if (assigned is null)
        {
            _logger.LogWarning("Cannot remove tag {Tag} from alert {AlertId}: not assigned", normalizedTag, id);
            return false;
        }

        alert.Tags.Remove(assigned);
        await _repository.UpdateAsync(alert, cancellationToken);

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", normalizedTag, id);
        return true;
    }

    private static List<string> NormalizeTagNames(IEnumerable<string> names)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in names)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length < AlertConstants.TagMinLength || trimmed.Length > AlertConstants.TagMaxLength)
            {
                throw new ValidationException($"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.");
            }

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}
