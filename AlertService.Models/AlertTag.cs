namespace AlertService.Models;

/// <summary>
/// Explicit join entity for the many-to-many relationship between alerts and tags.
/// </summary>
public class AlertTag
{
    public int AlertId { get; set; }

    public Alert Alert { get; set; } = null!;

    public int TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
