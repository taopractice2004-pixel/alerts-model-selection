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
    /// Returns daily alert-creation counts by severity for the last <c>request.Days</c> UTC calendar
    /// days (oldest first), with every day and severity zero-filled.
    /// </summary>
    Task<AlertTrendResponse> GetTrendsAsync(AlertTrendQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an alert, or suppresses it when an active alert with the same title
    /// (case-insensitive) and severity already exists within the configured window.
    /// </summary>
    /// <returns>The outcome status and the created or existing matching alert.</returns>
    Task<(AlertCreationStatus Status, AlertResponse Alert)> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds one or more tags to an alert (deduped case-insensitively, capped per alert).</summary>
    /// <returns>The operation status and, on success, the updated alert.</returns>
    Task<(TagOperationStatus Status, AlertResponse? Alert)> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag from an alert, matching the tag name case-insensitively.</summary>
    Task<TagOperationStatus> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}
