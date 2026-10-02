using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(AlertConstants.MaxTagsPerAlert)]
    public List<string> Tags { get; set; } = new();
}
