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
    /// Returns one zero-filled bucket per UTC calendar day for the requested number of days
    /// (oldest-first), with a total count and a per-severity breakdown reflecting alert
    /// <c>CreatedDate</c> (regardless of <c>IsActive</c>).
    /// </summary>
    Task<AlertTrendsResponse> GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new alert, unless an active alert with the same Title (case-insensitive) and
    /// Severity was created within the configured suppression window, in which case the existing
    /// alert is returned with <see cref="CreateAlertStatus.DuplicateSuppressed"/>.
    /// </summary>
    Task<CreateAlertResult> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default);

    /// <returns>The deactivated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the alert was deleted, <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one or more tags to an alert. Tag names are deduplicated case-insensitively against
    /// both the request and the alert's existing tags; the whole request is rejected if the
    /// resulting distinct tag count would exceed <see cref="AlertService.Common.Constants.AlertConstants.MaxTagsPerAlert"/>.
    /// </summary>
    Task<AddAlertTagsResult> AddTagsAsync(int id, AddAlertTagsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a single tag (matched case-insensitively) from an alert.
    /// </summary>
    /// <returns><c>true</c> if removed; <c>false</c> if the alert does not exist or has no such tag assigned.</returns>
    Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of <see cref="IAlertService.AddTagsAsync"/>.</summary>
public enum AddAlertTagsStatus
{
    Success,
    AlertNotFound,
    TooManyTags
}

/// <summary>Result of <see cref="IAlertService.AddTagsAsync"/>.</summary>
public record AddAlertTagsResult(AddAlertTagsStatus Status, AlertResponse? Alert);

/// <summary>Outcome of <see cref="IAlertService.CreateAsync"/>.</summary>
public enum CreateAlertStatus
{
    Created,
    DuplicateSuppressed
}

/// <summary>Result of <see cref="IAlertService.CreateAsync"/>.</summary>
public record CreateAlertResult(CreateAlertStatus Status, AlertResponse Alert);
