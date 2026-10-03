using AlertService.Common.Constants;

namespace AlertService.API.Configuration;

/// <summary>
/// Options controlling duplicate-alert suppression on create, bound from the
/// <see cref="SectionName"/> configuration section.
/// </summary>
public sealed class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    /// <summary>Window, in minutes, within which an active duplicate suppresses a new create.</summary>
    public int WindowMinutes { get; set; } = AlertConstants.DefaultSuppressionWindowMinutes;
}
