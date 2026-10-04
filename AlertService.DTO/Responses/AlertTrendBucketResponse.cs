namespace AlertService.DTO.Responses;

public class AlertTrendBucketResponse
{
    /// <summary>The UTC calendar day this bucket covers.</summary>
    public DateOnly Date { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
