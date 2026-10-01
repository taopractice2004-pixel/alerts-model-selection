namespace AlertService.DTO.Responses;

/// <summary>
/// Daily alert-creation counts bucketed per UTC calendar day, oldest-first.
/// </summary>
public class AlertTrendsResponse
{
    public List<AlertTrendBucketResponse> Buckets { get; set; } = new();
}

/// <summary>
/// Alert-creation counts for a single UTC calendar day, broken down by severity.
/// </summary>
public class AlertTrendBucketResponse
{
    /// <summary>The UTC calendar day this bucket represents (date-only, midnight UTC).</summary>
    public DateTime Date { get; set; }

    public int TotalCount { get; set; }

    public AlertSeverityCountsResponse SeverityCounts { get; set; } = new();
}
