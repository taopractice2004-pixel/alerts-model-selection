using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(AlertConstants.MaxTagsPerAlert)]
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
        {
            yield break;
        }

        for (var index = 0; index < Tags.Count; index++)
        {
            var tag = Tags[index];
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tags must contain non-whitespace values.",
                    [$"{nameof(Tags)}[{index}]"]);

                continue;
            }

            var trimmedTag = tag.Trim();
            if (trimmedTag.Length < AlertConstants.TagMinLength || trimmedTag.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.",
                    [$"{nameof(Tags)}[{index}]"]);
            }
        }
    }
}
