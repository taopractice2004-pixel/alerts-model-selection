using AlertService.DTO.Requests;
using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>
/// Business operations for alerts. Returns DTOs so controllers never see entities.
/// </summary>
public interface IAlertService
{
    Task<PagedResponse<AlertResponse>> GetAllAsync(AlertQueryRequest request, CancellationToken cancellationToken = default);

    Task<AlertResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AlertSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns exactly <see cref="AlertTrendQueryRequest.Days"/> UTC calendar-day buckets, oldest first,
    /// ending on the current UTC day. Each bucket carries the total alert-creation count and a
    /// per-severity breakdown, including days and severities with zero alerts.
    /// </summary>
    Task<AlertTrendResponse> GetTrendsAsync(AlertTrendQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an alert, or suppresses a near-duplicate. When an active alert with the same
    /// title (case-insensitive) and severity exists within the configured window, the existing
    /// alert is returned and <see cref="CreateAlertResult.Suppressed"/> is <c>true</c>.
    /// </summary>
    Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds one or more tags to an alert, deduping case-insensitively and enforcing the per-alert cap.</summary>
    Task<AddTagsResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a single tag assignment from an alert.</summary>
    Task<RemoveTagStatus> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a create-alert operation. <see cref="Suppressed"/> is <c>true</c> when an existing
/// near-duplicate alert was returned instead of creating a new one.
/// </summary>
public sealed record CreateAlertResult(bool Suppressed, AlertResponse Alert);

/// <summary>Outcome of an add-tags operation.</summary>
public enum AddTagsStatus
{
    Success,
    AlertNotFound,
    TagLimitExceeded
}

/// <summary>Result of an add-tags operation; <see cref="Alert"/> is set only on success.</summary>
public sealed record AddTagsResult(AddTagsStatus Status, AlertResponse? Alert);

/// <summary>Outcome of a remove-tag operation.</summary>
public enum RemoveTagStatus
{
    Removed,
    AlertNotFound,
    TagNotFound
}
