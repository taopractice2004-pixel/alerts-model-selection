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
            var tag = Tags[i];
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tag must contain at least one non-whitespace character.",
                    [$"{nameof(Tags)}[{i}]"]);
                continue;
            }

            if (tag.Trim().Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag length must be between 1 and {AlertConstants.TagMaxLength} characters.",
                    [$"{nameof(Tags)}[{i}]"]);
            }
        }
    }
}