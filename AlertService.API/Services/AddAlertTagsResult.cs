using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AddAlertTagsOutcome
{
    Success,
    NotFound,
    Invalid
}

/// <summary>Outcome of adding tags; <see cref="Error"/> is set only when <see cref="Outcome"/> is Invalid.</summary>
public sealed record AddAlertTagsResult(AddAlertTagsOutcome Outcome, AlertResponse? Alert = null, string? Error = null)
{
    public static AddAlertTagsResult Success(AlertResponse alert) => new(AddAlertTagsOutcome.Success, alert);

    public static AddAlertTagsResult NotFound() => new(AddAlertTagsOutcome.NotFound);

    public static AddAlertTagsResult Invalid(string error) => new(AddAlertTagsOutcome.Invalid, Error: error);
}
