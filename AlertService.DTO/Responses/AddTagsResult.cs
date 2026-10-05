namespace AlertService.DTO.Responses;

public enum AddTagsStatus
{
    Success,
    AlertNotFound,
    TagLimitExceeded
}

/// <summary>
/// Outcome of an add-tags operation so the controller can map it to the right HTTP status
/// without the service taking an HTTP dependency.
/// </summary>
public sealed class AddTagsResult
{
    public AddTagsStatus Status { get; init; }

    public AlertResponse? Alert { get; init; }
}
