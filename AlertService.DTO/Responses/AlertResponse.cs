using AlertService.Common.Enums;

namespace AlertService.DTO.Responses;

public class AlertResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Severity Severity { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }

    public List<string> Tags { get; set; } = [];
}
