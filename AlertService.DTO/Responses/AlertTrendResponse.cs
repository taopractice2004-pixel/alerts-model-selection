namespace AlertService.DTO.Responses;

public class AlertTrendResponse
{
    public string Date { get; set; } = string.Empty;

    public int Low { get; set; }

    public int Medium { get; set; }

    public int High { get; set; }

    public int Critical { get; set; }
}