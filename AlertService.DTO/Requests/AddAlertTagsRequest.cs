using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public string[] Tags { get; set; } = Array.Empty<string>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null || Tags.Length == 0)
        {
            yield return new ValidationResult("At least one tag is required.", new[] { nameof(Tags) });
            yield break;
        }

        for (var index = 0; index < Tags.Length; index++)
        {
            var tag = Tags[index];
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult("Tag values must contain non-whitespace characters.", new[] { $"{nameof(Tags)}[{index}]" });
                continue;
            }

            if (tag.Trim().Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag values cannot exceed {AlertConstants.TagMaxLength} characters.",
                    new[] { $"{nameof(Tags)}[{index}]" });
            }
        }
    }
}