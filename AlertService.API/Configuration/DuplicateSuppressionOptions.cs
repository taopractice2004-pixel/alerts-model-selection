using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Configuration;

public class DuplicateSuppressionOptions
{
    public const string SectionName = "AlertSuppression";

    /// <summary>Suppression window in minutes; 0 disables duplicate suppression.</summary>
    [Range(0, int.MaxValue)]
    public int DuplicateWindowMinutes { get; set; } = 15;
}
