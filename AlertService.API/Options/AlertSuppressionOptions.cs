using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Options;

public sealed class AlertSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    [Range(1, int.MaxValue)]
    public int DuplicateWindowMinutes { get; init; }
}