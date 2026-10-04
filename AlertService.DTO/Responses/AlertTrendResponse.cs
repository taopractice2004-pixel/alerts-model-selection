namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public int Days { get; set; }

    /// <summary>One bucket per UTC calendar day, ordered oldest day first.</summary>
    public IReadOnlyList<AlertTrendBucketResponse> Buckets { get; set; } = new List<AlertTrendBucketResponse>();
}
