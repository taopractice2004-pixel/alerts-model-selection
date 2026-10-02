using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AddTagsStatus
{
    Added,
    AlertNotFound,
    TagLimitExceeded
}

public record AddTagsResult(AddTagsStatus Status, AlertResponse? Alert = null);
