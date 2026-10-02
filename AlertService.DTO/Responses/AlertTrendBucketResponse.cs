namespace AlertService.DTO.Responses;

public class AlertTrendBucketResponse
{
    public DateOnly Day { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}