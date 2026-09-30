namespace AlertService.API.Configurations;

public class AlertDeduplicationOptions
{
    /// <summary>
    /// Suppression window in minutes. Alerts with identical title+severity created within
    /// this window will be considered duplicates and suppressed.
    /// </summary>
    public int SuppressionWindowMinutes { get; set; } = 15;
}
