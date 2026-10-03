namespace AlertService.DTO.Responses;

public class AlertTrendsResponse
{
    public IReadOnlyList<AlertTrendBucketResponse> Buckets { get; set; } = new List<AlertTrendBucketResponse>();
}
