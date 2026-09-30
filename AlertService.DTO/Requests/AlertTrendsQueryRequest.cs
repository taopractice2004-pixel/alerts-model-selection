using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTrendsQueryRequest
{
    [Range(AlertConstants.TrendsMinDays, AlertConstants.TrendsMaxDays)]
    public int Days { get; set; } = AlertConstants.TrendsDefaultDays;
}
