namespace AlertService.API.Configuration;

/// <summary>
/// Options for near-duplicate alert suppression, bound from the "AlertSuppression" section.
/// </summary>
public sealed class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    // Suppression window in minutes. A value <= 0 disables suppression.
    public int WindowMinutes { get; set; } = 15;
}
