using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public record CreateAlertResult(AlertResponse Alert, bool IsDuplicateSuppressed);
