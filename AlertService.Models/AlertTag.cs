namespace AlertService.Models;

public class AlertTag
{
    public int AlertId { get; set; }

    public int TagId { get; set; }

    public Alert Alert { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}