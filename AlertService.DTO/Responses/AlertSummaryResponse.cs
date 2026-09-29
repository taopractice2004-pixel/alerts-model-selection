namespace AlertService.DTO.Responses;

public class AlertSummaryResponse
{
    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}