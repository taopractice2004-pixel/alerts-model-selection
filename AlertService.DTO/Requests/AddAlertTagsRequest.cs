using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
        {
            yield return new ValidationResult(
                "At least one tag is required.",
                [nameof(Tags)]);

            yield break;
        }

        if (Tags.Count == 0)
        {
            yield return new ValidationResult(
                "At least one tag is required.",
                [nameof(Tags)]);

            yield break;
        }

        for (var index = 0; index < Tags.Count; index++)
        {
            var tag = Tags[index];

            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tags cannot be blank.",
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