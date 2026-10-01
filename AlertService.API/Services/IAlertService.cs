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
    /// Gets daily alert-creation counts bucketed per UTC calendar day for the requested number of
    /// days (ending on the current UTC day, inclusive), broken down by severity. Buckets are
    /// oldest-first and include zero-count days and severities.
    /// </summary>
    Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new alert, or suppresses creation when a near-duplicate active alert (same
    /// trimmed, case-insensitive title and severity) exists within the configured recent window.
    /// </summary>
    /// <returns>
    /// An <see cref="AlertCreateResult"/> wrapping the created or existing alert and whether
    /// creation was suppressed.
    /// </returns>
    Task<AlertCreateResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds one or more tags to an alert (trimmed, case-insensitive dedup, max per alert enforced).</summary>
    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    /// <exception cref="AlertValidationException">Thrown when the per-alert tag limit would be exceeded.</exception>
    Task<AlertResponse?> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag from an alert (case-insensitive).</summary>
    /// <returns><c>true</c> if removed; <c>false</c> if the alert or the tag assignment was missing.</returns>
    Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}
