namespace AlertService.DTO.Responses;

/// <summary>Outcome of an alert create operation, mapped by the controller to an HTTP status.</summary>
public enum AlertCreationStatus
{
    Created,
    DuplicateSuppressed
}
