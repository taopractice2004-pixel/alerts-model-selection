namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts. Names are unique case-insensitively.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
