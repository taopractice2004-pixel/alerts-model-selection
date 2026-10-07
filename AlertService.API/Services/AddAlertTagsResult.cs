using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>Outcome of adding tags: <see cref="Alert"/> is null when the alert is missing or the tag limit was exceeded.</summary>
public sealed record AddAlertTagsResult(AlertResponse? Alert, bool TagLimitExceeded);
