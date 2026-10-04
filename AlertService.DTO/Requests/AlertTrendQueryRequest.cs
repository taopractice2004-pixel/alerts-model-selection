using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AlertTrendQueryRequest
{
    public const int DefaultDays = 7;
    public const int MaxDays = 90;

    [Range(1, MaxDays)]
    public int Days { get; set; } = DefaultDays;
}