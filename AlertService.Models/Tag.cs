namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    /// <summary>
    /// Canonical stored form of a tag name: trimmed and lower-cased.
    /// </summary>
    public static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
