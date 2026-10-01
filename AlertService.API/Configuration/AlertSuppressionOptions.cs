namespace AlertService.API.Configuration;

/// <summary>
/// Strongly-typed options controlling near-duplicate alert suppression on alert creation.
/// </summary>
public class AlertSuppressionOptions
{
    /// <summary>Configuration section name bound from application settings.</summary>
    public const string SectionName = "AlertSuppression";

    /// <summary>
    /// Recent window, in minutes, used to detect a near-duplicate active alert with the
    /// same title and severity. Defaults to 15 minutes when not configured.
    /// </summary>
    public int WindowMinutes { get; set; } = 15;
}
