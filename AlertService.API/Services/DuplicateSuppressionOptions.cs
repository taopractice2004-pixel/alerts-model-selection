namespace AlertService.API.Services;

public class DuplicateSuppressionOptions
{
    public const string SectionName = "DuplicateSuppression";

    /// <summary>Suppression window in minutes; a value of 0 or less disables suppression.</summary>
    public int WindowMinutes { get; set; } = 15;
}
