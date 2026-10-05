using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(AlertConstants.MaxTagsPerRequest)]
    public List<string> Tags { get; set; } = new();
}
