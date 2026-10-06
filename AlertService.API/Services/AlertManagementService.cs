using AlertService.API.Mappings;
using AlertService.Common.Constants;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace AlertService.API.Services;

// Named AlertManagementService (not "AlertService") to avoid clashing with the root namespace.
public class AlertManagementService : IAlertService
{
    private const string DuplicateSuppressionWindowMinutesKey = "Alerts:DuplicateSuppressionWindowMinutes";

    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertManagementService> _logger;
    private readonly IConfiguration _configuration;

    public AlertManagementService(
        IAlertRepository repository,
        TimeProvider timeProvider,
        ILogger<AlertManagementService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _configuration = configuration;
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
            NormalizeOptionalTag(request.Tag),
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

    public async Task<(AlertResponse Alert, bool DuplicateSuppressed)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var suppressionWindowMinutes = GetDuplicateSuppressionWindowMinutes();
        var createdAfterUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-suppressionWindowMinutes);
        var existingAlert = await _repository.FindActiveDuplicateAsync(
            request.Title,
            request.Severity,
            createdAfterUtc,
            cancellationToken);

        if (existingAlert is not null)
        {
            _logger.LogInformation(
                "Suppressed duplicate alert for title {AlertTitle} and severity {Severity} in favor of alert {AlertId}",
                existingAlert.Title,
                existingAlert.Severity,
                existingAlert.Id);

            return (existingAlert.ToResponse(), true);
        }

        var alert = request.ToEntity(_timeProvider.GetUtcNow().UtcDateTime);
        var created = await _repository.AddAsync(alert, cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with severity {Severity}", created.Id, created.Severity);
        return (created.ToResponse(), false);
    }

    public async Task<AlertTagAddResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTags = NormalizeTags(request.Tags);
        if (normalizedTags.Length == 0)
        {
            return new AlertTagAddResult
            {
                ValidationError = "At least one tag is required."
            };
        }

        if (normalizedTags.Any(tag => !Regex.IsMatch(tag, AlertConstants.TagRouteSafePattern)))
        {
            return new AlertTagAddResult
            {
                ValidationError = AlertConstants.TagRouteSafeMessage
            };
        }

        var alert = await _repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            _logger.LogWarning("Cannot add tags to alert {AlertId}: not found", id);
            return new AlertTagAddResult
            {
                AlertNotFound = true
            };
        }

        var totalUniqueTags = alert.Tags
            .Select(tag => tag.Name)
            .Concat(normalizedTags)
            .Distinct(StringComparer.Ordinal)
            .Count();

        if (totalUniqueTags > AlertConstants.MaxTagsPerAlert)
        {
            return new AlertTagAddResult
            {
                ValidationError = $"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags."
            };
        }

        await _repository.AddTagsAsync(alert, normalizedTags, cancellationToken);

        _logger.LogInformation("Added {TagCount} tags to alert {AlertId}", normalizedTags.Length, id);
        return new AlertTagAddResult
        {
            Alert = alert.ToResponse()
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

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var removed = await _repository.RemoveTagAsync(id, NormalizeTag(tag), cancellationToken);
        if (!removed)
        {
            _logger.LogWarning("Cannot remove tag from alert {AlertId}: alert or tag was not found", id);
            return false;
        }

        _logger.LogInformation("Removed tag {Tag} from alert {AlertId}", tag, id);
        return true;
    }

    private static string[] NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(NormalizeTag)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? NormalizeOptionalTag(string? tag)
    {
        return string.IsNullOrWhiteSpace(tag) ? null : NormalizeTag(tag);
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToLowerInvariant();
    }

    private int GetDuplicateSuppressionWindowMinutes()
    {
        var suppressionWindowMinutes = _configuration.GetValue<int?>(DuplicateSuppressionWindowMinutesKey);
        if (!suppressionWindowMinutes.HasValue || suppressionWindowMinutes.Value <= 0)
        {
            throw new InvalidOperationException($"Configuration value '{DuplicateSuppressionWindowMinutesKey}' must be a positive integer.");
        }

        return suppressionWindowMinutes.Value;
    }
}
