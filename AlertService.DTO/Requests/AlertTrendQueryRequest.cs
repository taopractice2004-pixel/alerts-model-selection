using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTrendQueryRequest
{
    [Range(AlertConstants.TrendMinDays, AlertConstants.TrendMaxDays)]
    public int Days { get; set; } = AlertConstants.TrendDefaultDays;
}
