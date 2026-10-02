namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts. Names are stored trimmed and lower-case.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
