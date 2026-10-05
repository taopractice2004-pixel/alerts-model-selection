using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null || Tags.Count == 0)
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

            if (tag.Trim().Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tags cannot exceed {AlertConstants.TagMaxLength} characters.",
                    [$"{nameof(Tags)}[{index}]"]);
            }
        }
    }
}
