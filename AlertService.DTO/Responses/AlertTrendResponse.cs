namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public IReadOnlyList<AlertTrendDayResponse> Days { get; set; } = [];
}
