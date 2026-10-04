namespace AlertService.DTO.Responses;

/// <summary>Outcome of a tag add/remove operation, mapped by the controller to an HTTP status.</summary>
public enum TagOperationStatus
{
    Success,
    AlertNotFound,
    TagLimitExceeded,
    TagNotAssigned
}
