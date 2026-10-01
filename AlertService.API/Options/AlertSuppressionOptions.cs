namespace AlertService.API.Options;

/// <summary>
/// Configuration for near-duplicate alert suppression, bound from the
/// <c>AlertSuppression</c> section of appsettings.json.
/// </summary>
public class AlertSuppressionOptions
{
    /// <summary>
    /// The window, in minutes, within which an active alert with the same Title
    /// (case-insensitive) and Severity suppresses creation of a new alert.
    /// </summary>
    public int DuplicateWindowMinutes { get; set; } = 15;
}
