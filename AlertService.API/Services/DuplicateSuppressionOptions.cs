namespace AlertService.API.Services;

public sealed class DuplicateSuppressionOptions
{
    public const string SectionName = "AlertDuplicateSuppression";

    public int WindowMinutes { get; set; }
}
