using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;
using AlertService.Common.Enums;

namespace AlertService.DTO.Requests;

public class UpdateAlertRequest
{
    [Required]
    [RegularExpression(AlertConstants.NonWhitespacePattern)]
    [StringLength(AlertConstants.TitleMaxLength, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(AlertConstants.DescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(Severity))]
    public Severity Severity { get; set; }

    public bool IsActive { get; set; }
}
