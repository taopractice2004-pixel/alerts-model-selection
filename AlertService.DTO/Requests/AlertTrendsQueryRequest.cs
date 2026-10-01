using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTrendsQueryRequest
{
    [Range(AlertConstants.MinTrendsDays, AlertConstants.MaxTrendsDays)]
    public int Days { get; set; } = AlertConstants.DefaultTrendsDays;
}
