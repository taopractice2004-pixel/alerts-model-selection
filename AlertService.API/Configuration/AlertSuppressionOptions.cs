using AlertService.Common.Constants;

namespace AlertService.API.Configuration;

/// <summary>
/// Options for duplicate-alert suppression, bound from the <c>AlertSuppression</c> configuration section.
/// </summary>
public class AlertSuppressionOptions
{
    /// <summary>Length of the suppression window in minutes.</summary>
    public int WindowMinutes { get; set; } = AlertConstants.DefaultSuppressionWindowMinutes;
}
