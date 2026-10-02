namespace AlertService.Models;

/// <summary>
/// Tag domain entity. A tag is shared across alerts through a many-to-many relationship.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
