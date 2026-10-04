using AlertService.Models;

namespace AlertService.Data.Interfaces;

public class AlertTagMutationResult
{
    public AlertTagMutationStatus Status { get; init; }

    public Alert? Alert { get; init; }

    public Dictionary<string, string[]> Errors { get; init; } = new(StringComparer.Ordinal);
}