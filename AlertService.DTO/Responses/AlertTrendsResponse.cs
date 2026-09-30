namespace AlertService.DTO.Responses;

public class AlertTrendsResponse
{
    public List<DailyAlertTrendResponse> DailyTrends { get; set; } = new();
}
