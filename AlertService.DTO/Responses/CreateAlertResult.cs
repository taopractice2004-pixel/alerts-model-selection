namespace AlertService.DTO.Responses;

public enum CreateAlertStatus
{
    Created,
    Suppressed
}

/// <summary>
/// Outcome of a create-alert operation so the controller can map it to the right HTTP status
/// (201 Created vs 200 OK duplicate suppression) without the service taking an HTTP dependency.
/// </summary>
public sealed class CreateAlertResult
{
    public CreateAlertStatus Status { get; init; }

    public AlertResponse Alert { get; init; } = null!;
}
