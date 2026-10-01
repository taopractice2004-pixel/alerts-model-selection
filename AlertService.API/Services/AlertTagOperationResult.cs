using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public sealed record AlertTagOperationResult(
    AlertTagOperationStatus Status,
    AlertResponse? Alert = null,
    string? ErrorMessage = null)
{
    public static AlertTagOperationResult Success(AlertResponse alert) => new(AlertTagOperationStatus.Success, alert);

    public static AlertTagOperationResult NotFound() => new(AlertTagOperationStatus.NotFound);

    public static AlertTagOperationResult ValidationFailed(string errorMessage) => new(AlertTagOperationStatus.ValidationFailed, null, errorMessage);
}
