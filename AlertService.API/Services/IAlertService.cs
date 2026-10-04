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

    /// <summary>Gets one zero-filled bucket per UTC day for the last N days (oldest first, ending today), with total and per-severity counts.</summary>
    Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates an alert, or returns the existing one (<see cref="CreateAlertResult.IsDuplicate"/>) when an identical active alert was created within the suppression window.</summary>
    Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds tags (trimmed, de-duplicated case-insensitively) to an alert, enforcing the per-alert tag limit.</summary>
    Task<AddTagsResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the tag was removed, <c>false</c> if the alert or the tag assignment was not found.</returns>
    Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}
