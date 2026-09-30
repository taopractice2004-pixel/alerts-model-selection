namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<AlertTag> AlertTags { get; set; } = new List<AlertTag>();
}
