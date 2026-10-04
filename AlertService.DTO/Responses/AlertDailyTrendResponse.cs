namespace AlertService.DTO.Responses;

public class AlertDailyTrendResponse
{
    public DateTime DayUtc { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}