using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AddAlertTagsStatus
{
    Success,
    AlertNotFound,
    TooManyTags
}

public sealed record AddAlertTagsResult(AddAlertTagsStatus Status, AlertResponse? Alert = null);
