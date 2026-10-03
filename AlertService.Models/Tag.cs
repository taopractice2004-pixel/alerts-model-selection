namespace AlertService.Models;

/// <summary>
/// Reusable alert tag entity with normalized value for case-insensitive matching.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
