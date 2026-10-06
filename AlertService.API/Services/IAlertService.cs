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
    /// Creates an alert, or suppresses it when it is a near-duplicate of a recent active alert.
    /// The result's <see cref="CreateAlertResult.Status"/> tells the controller whether to return
    /// 201 Created or a 200 OK duplicate-suppressed response.
    /// </summary>
    Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds one or more tags to an alert, deduping case-insensitively.</summary>
    Task<AddTagsResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the tag assignment was removed, <c>false</c> if the alert or
    /// assignment was not found.</returns>
    Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}
