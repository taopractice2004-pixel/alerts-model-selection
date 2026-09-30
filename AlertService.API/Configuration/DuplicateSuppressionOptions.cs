namespace AlertService.API.Configuration;

/// <summary>
/// Options controlling near-duplicate alert suppression on create. Bound from configuration
/// section <see cref="SectionName"/>.
/// </summary>
public sealed class DuplicateSuppressionOptions
{
    public const string SectionName = "Alerts:DuplicateSuppression";

    /// <summary>
    /// Window, in minutes, within which an existing active alert with the same title and
    /// severity suppresses a new create. Defaults to 15 when the setting is absent.
    /// </summary>
    public int WindowMinutes { get; set; } = 15;
}
