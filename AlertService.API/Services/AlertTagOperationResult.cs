using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AlertTagOperationStatus
{
    Success,
    AlertNotFound,
    TagAlreadyAssigned,
    TagNotAssigned,
    MaxTagsReached,
    InvalidTag
}

public sealed class AlertTagOperationResult
{
    public AlertTagOperationStatus Status { get; init; }

    public AlertResponse? Alert { get; init; }
}
