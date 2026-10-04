using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(AlertConstants.MaxTagsPerAlert)]
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var i = 0; i < Tags.Count; i++)
        {
            var value = Tags[i]?.Trim() ?? string.Empty;
            if (value.Length is < 1 or > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between 1 and {AlertConstants.TagMaxLength} characters.",
                    [$"{nameof(Tags)}[{i}]"]);
            }
        }
    }
}
