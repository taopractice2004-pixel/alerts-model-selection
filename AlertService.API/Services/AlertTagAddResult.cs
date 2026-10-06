using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public class AlertTagAddResult
{
    public AlertResponse? Alert { get; init; }

    public bool AlertNotFound { get; init; }

    public string? ValidationError { get; init; }
}