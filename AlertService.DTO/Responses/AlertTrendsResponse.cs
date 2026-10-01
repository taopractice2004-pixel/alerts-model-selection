namespace AlertService.DTO.Responses;

public class AlertTrendsResponse
{
    public int Days { get; set; }

    public IReadOnlyList<AlertTrendBucketResponse> Buckets { get; set; } = [];
}
