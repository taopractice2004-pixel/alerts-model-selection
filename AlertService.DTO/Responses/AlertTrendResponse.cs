namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public DateOnly Date { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
