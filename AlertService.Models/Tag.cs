namespace AlertService.Models;

/// <summary>
/// Free-form tag that can be attached to many alerts (many-to-many with <see cref="Alert"/>).
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
