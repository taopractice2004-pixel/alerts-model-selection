namespace AlertService.DTO.Responses;

/// <summary>Alert creation counts for a single UTC calendar day.</summary>
public class AlertTrendBucketResponse
{
    public DateOnly Date { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
