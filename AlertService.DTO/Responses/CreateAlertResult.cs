namespace AlertService.DTO.Responses;

public class CreateAlertResult
{
    public AlertResponse Alert { get; set; } = new AlertResponse();

    /// <summary>True when a duplicate was detected and the existing alert was returned instead of creating a new one.</summary>
    public bool Suppressed { get; set; }

    /// <summary>True when a new alert was created (201). False when an existing alert was returned (200).</summary>
    public bool IsNew { get; set; }
}
