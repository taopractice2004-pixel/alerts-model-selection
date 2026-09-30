using AlertService.Common.Enums;

namespace AlertService.Models;

/// <summary>
/// Alert domain entity persisted to the database.
/// </summary>
public class Alert
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Severity Severity { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }

    public ICollection<AlertTag> AlertTags { get; set; } = [];
}
