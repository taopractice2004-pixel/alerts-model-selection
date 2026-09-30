namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public int Days { get; set; }

    public List<AlertTrendBucketResponse> Buckets { get; set; } = [];
}