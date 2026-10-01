using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>
/// Outcome of an alert creation request. <paramref name="WasSuppressed"/> is <c>true</c> when an
/// existing near-duplicate active alert was returned instead of creating a new row.
/// </summary>
/// <param name="Alert">The created alert, or the existing alert when suppressed.</param>
/// <param name="WasSuppressed">Whether creation was suppressed as a near-duplicate.</param>
public sealed record AlertCreateResult(AlertResponse Alert, bool WasSuppressed);
