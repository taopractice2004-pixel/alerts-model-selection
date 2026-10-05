namespace AlertService.Models;

/// <summary>
/// Free-form label that can be attached to many alerts.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    /// <summary>First-seen casing, used for display.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Trimmed, lower-invariant key used for case-insensitive matching.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
