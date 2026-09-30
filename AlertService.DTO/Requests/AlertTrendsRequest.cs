using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AlertTrendsRequest
{
    [Range(1, 365)]
    public int Days { get; set; } = 7;
}
