using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>Outcome of an add-tags operation, distinguishing success, missing alert, and validation failure.</summary>
public enum AddTagsOutcome
{
    Success,
    AlertNotFound,
    Invalid
}

public sealed record AddTagsResult(AddTagsOutcome Outcome, AlertResponse? Alert, string? Error)
{
    public static AddTagsResult Succeeded(AlertResponse alert) => new(AddTagsOutcome.Success, alert, null);

    public static AddTagsResult NotFound() => new(AddTagsOutcome.AlertNotFound, null, null);

    public static AddTagsResult Invalid(string error) => new(AddTagsOutcome.Invalid, null, error);
}
