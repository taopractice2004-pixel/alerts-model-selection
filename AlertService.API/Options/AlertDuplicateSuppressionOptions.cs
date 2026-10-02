namespace AlertService.API.Options;

public class AlertDuplicateSuppressionOptions
{
    public const string SectionName = "Alerts";

    public int DuplicateSuppressionWindowMinutes { get; set; }
}