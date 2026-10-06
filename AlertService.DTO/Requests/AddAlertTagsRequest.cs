using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest
{
    [Required]
    [MinLength(1)]
    public List<string> Tags { get; set; } = new();
}