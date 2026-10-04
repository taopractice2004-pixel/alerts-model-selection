using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public enum AddTagsOutcome
{
    Added,
    AlertNotFound,
    TagLimitExceeded
}

/// <summary>Result of adding tags; <see cref="Alert"/> is set only when <see cref="Outcome"/> is <see cref="AddTagsOutcome.Added"/>.</summary>
public sealed record AddTagsResult(AddTagsOutcome Outcome, AlertResponse? Alert = null);
