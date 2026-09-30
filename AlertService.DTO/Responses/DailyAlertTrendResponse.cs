namespace AlertService.DTO.Responses;

public class DailyAlertTrendResponse
{
    public DateTime Date { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
