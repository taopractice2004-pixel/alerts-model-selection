namespace AlertService.DTO.Responses;

public class AlertTrendBucketResponse
{
    public DateTime DateUtc { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
