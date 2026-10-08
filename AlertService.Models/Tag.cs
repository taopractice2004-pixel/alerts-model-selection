namespace AlertService.Models;

public class Tag
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public ICollection<AlertTag> AlertTags { get; set; } = new List<AlertTag>();
}