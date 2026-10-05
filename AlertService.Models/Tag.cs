namespace AlertService.Models;

/// <summary>
/// Free-form label shared by many alerts. Unique by name (case-insensitive).
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Alert> Alerts { get; set; } = new();
}
