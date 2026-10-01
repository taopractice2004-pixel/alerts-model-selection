namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts. The stored <see cref="Name"/> holds a
/// single canonical (trimmed, first-seen casing) representation; uniqueness is enforced
/// case-insensitively via a unique index.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
