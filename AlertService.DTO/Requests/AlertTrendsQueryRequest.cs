using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AlertTrendsQueryRequest
{
    private const int DefaultTrendDays = 7;
    private const int MaximumTrendDays = 90;

    [Range(1, MaximumTrendDays)]
    public int Days { get; set; } = DefaultTrendDays;
}
