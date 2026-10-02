namespace AlertService.Models;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<AlertTag> AlertTags { get; set; } = new List<AlertTag>();
}