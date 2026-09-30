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

    /// <summary>Gets per-day alert creation trends, oldest first, for the requested day count.</summary>
    Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default);

    /// <returns>
    /// The created alert, or an existing near-duplicate alert if creation was suppressed (see
    /// <see cref="CreateAlertResult.IsDuplicate"/>).
    /// </returns>
    Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <returns>The alert with the new tags attached, or <c>null</c> if no alert with the given id exists.</returns>
    /// <exception cref="TagLimitExceededException">The alert would end up with more than the maximum allowed tags.</exception>
    Task<AlertResponse?> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <returns>The alert with the tag removed, or <c>null</c> if the alert or tag assignment does not exist.</returns>
    Task<AlertResponse?> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}
