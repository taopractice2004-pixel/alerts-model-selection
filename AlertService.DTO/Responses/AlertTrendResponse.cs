namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public List<AlertTrendDayResponse> Days { get; set; } = [];
}

public class AlertTrendDayResponse
{
    public DateOnly Date { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}