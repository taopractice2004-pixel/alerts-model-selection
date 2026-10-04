using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTrendsQueryRequest
{
    [Range(AlertConstants.MinTrendDays, AlertConstants.MaxTrendDays)]
    public int Days { get; set; } = AlertConstants.DefaultTrendDays;
}
