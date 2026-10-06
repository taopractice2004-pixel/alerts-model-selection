namespace AlertService.DTO.Responses;

public class AlertTrendsResponse
{
    public int Days { get; set; }

    public IReadOnlyList<AlertDailyTrendResponse> Trends { get; set; } = new List<AlertDailyTrendResponse>();
}
