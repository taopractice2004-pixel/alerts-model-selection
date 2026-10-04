using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Configuration;

public sealed class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    /// <summary>Minutes during which an identical active alert (same title and severity) suppresses a new one.</summary>
    [Range(1, 1440)]
    public int DuplicateWindowMinutes { get; set; } = 15;
}
