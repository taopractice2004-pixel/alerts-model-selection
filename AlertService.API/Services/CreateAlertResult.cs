using AlertService.DTO.Responses;

namespace AlertService.API.Services;

public class CreateAlertResult
{
    public required AlertResponse Alert { get; init; }

    public bool DuplicateSuppressed { get; init; }
}