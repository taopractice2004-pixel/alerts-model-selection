namespace AlertService.Models;

/// <summary>
/// Free-form label shared across alerts via a many-to-many relationship.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    // Stored normalized (trimmed, lowercased); uniqueness enforced by a unique index.
    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
