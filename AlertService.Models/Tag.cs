namespace AlertService.Models;

/// <summary>
/// Free-form tag that can be attached to many alerts (many-to-many). Tag names are unique
/// case-insensitively and shared/reused across alerts.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
