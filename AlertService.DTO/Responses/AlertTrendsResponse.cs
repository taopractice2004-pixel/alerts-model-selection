namespace AlertService.DTO.Responses;

public class AlertTrendsResponse
{
    public int Days { get; set; }

    public List<AlertTrendBucketResponse> Buckets { get; set; } = [];
}
