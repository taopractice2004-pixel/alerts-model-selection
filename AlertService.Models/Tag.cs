namespace AlertService.Models;

/// <summary>
/// A shared, free-form label that can be attached to one or more alerts. Tag rows are global:
/// the same tag text is never duplicated, so alerts referencing the same word share one row.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<AlertTag> AlertTags { get; set; } = new();
}
