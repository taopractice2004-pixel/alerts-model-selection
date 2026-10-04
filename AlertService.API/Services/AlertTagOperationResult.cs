using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public class AlertTagOperationResult
{
    public AlertTagOperationStatus Status { get; init; }

    public AlertResponse? Alert { get; init; }

    public Dictionary<string, string[]> Errors { get; init; } = new(StringComparer.Ordinal);
}