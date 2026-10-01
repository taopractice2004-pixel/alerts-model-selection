using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagRequest
{
    [Required]
    [RegularExpression(AlertConstants.NonWhitespacePattern)]
    [StringLength(AlertConstants.TagMaxLength, MinimumLength = 1)]
    public string Tag { get; set; } = string.Empty;
}
