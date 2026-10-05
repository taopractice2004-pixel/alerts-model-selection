namespace AlertService.Models;

/// <summary>
/// Tag entity that can be associated with multiple alerts.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = [];
}