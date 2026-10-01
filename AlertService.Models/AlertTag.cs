namespace AlertService.Models;

/// <summary>
/// Explicit join entity between <see cref="Alert"/> and <see cref="Tag"/>. Modeled explicitly
/// (rather than relying on EF Core's implicit skip-navigation join table) to match the repo's
/// existing explicit-configuration-class pattern (see <c>AlertConfiguration</c>).
/// </summary>
public class AlertTag
{
    public int AlertId { get; set; }

    public Alert Alert { get; set; } = null!;

    public int TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
