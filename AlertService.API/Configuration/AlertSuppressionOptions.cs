namespace AlertService.API.Configuration;

/// <summary>Configuration for near-duplicate alert suppression on alert creation.</summary>
public class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    /// <summary>Window in minutes during which a matching active alert suppresses a duplicate.</summary>
    public int WindowMinutes { get; set; } = 15;
}
