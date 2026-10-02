using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>
/// Outcome of creating an alert: the resulting alert and whether it was suppressed as a duplicate.
/// </summary>
public record CreateAlertResult(AlertResponse Alert, bool WasSuppressed);
