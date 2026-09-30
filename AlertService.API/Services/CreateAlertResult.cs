using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public sealed record CreateAlertResult(AlertResponse Alert, bool IsDuplicateSuppressed);