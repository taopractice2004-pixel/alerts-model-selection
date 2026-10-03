namespace AlertService.API.Options;

public sealed class DuplicateAlertOptions
{
    public const string SectionName = "Alerts";

    public int DuplicateSuppressionWindowMinutes { get; set; }
}