using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AddAlertTagsStatus
{
    Added,
    AlertNotFound,
    TagLimitExceeded
}

public record AddAlertTagsResult(AddAlertTagsStatus Status, AlertResponse? Alert);
