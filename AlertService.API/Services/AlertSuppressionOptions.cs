namespace AlertService.API.Services;

public class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    /// <summary>Minutes within which an active alert with the same title and severity suppresses a new one; 0 or less disables suppression.</summary>
    public int DuplicateWindowMinutes { get; set; }
}
