namespace AlertService.Data.Interfaces;

public class DailyAlertTrend
{
    public DateTime Date { get; set; }

    public int Low { get; set; }

    public int Medium { get; set; }

    public int High { get; set; }

    public int Critical { get; set; }
}
