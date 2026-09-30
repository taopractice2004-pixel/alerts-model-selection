using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AlertTrendRequest
{
    [Range(1, 90)]
    public int Days { get; set; } = 7;
}