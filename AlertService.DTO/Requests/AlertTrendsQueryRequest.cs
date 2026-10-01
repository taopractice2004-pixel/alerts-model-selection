using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTrendsQueryRequest
{
    /// <summary>
    /// Number of UTC calendar days to report, ending on the current UTC day (inclusive).
    /// </summary>
    [Range(AlertConstants.MinTrendDays, AlertConstants.MaxTrendDays)]
    public int Days { get; set; } = AlertConstants.DefaultTrendDays;
}
