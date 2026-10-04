using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum TagAssignmentStatus
{
    Success,
    AlertNotFound,
    MaxTagsExceeded
}

public sealed class TagAssignmentResult
{
    private TagAssignmentResult(TagAssignmentStatus status, AlertResponse? alert)
    {
        Status = status;
        Alert = alert;
    }

    public TagAssignmentStatus Status { get; }

    public AlertResponse? Alert { get; }

    public static TagAssignmentResult Success(AlertResponse alert) => new(TagAssignmentStatus.Success, alert);

    public static TagAssignmentResult AlertNotFound() => new(TagAssignmentStatus.AlertNotFound, null);

    public static TagAssignmentResult MaxTagsExceeded() => new(TagAssignmentStatus.MaxTagsExceeded, null);
}
