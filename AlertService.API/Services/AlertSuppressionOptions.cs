namespace AlertService.API.Services;

public sealed class AlertSuppressionOptions
{
    public const string SectionName = "Alerts";

    public const int DefaultDuplicateSuppressionWindowMinutes = 15;

    public int DuplicateSuppressionWindowMinutes { get; init; } = DefaultDuplicateSuppressionWindowMinutes;
}