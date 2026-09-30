namespace AlertService.Models;

/// <summary>
/// Tag domain entity representing a globally shared tag name.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    // Preserved casing for display
    public string Name { get; set; } = string.Empty;

    // Normalized lowercase value for uniqueness and lookups
    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
