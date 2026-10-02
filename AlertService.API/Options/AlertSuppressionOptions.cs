using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Options;

public sealed class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    [Range(1, 1440)]
    public int WindowMinutes { get; set; } = 15;
}
