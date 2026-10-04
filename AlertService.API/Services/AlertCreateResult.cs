using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public sealed class AlertCreateResult
{
    public required AlertResponse Alert { get; init; }

    public bool DuplicateSuppressed { get; init; }
}