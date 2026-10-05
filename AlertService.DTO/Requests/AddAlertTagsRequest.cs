using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags.Count == 0)
        {
            yield return new ValidationResult(
                "At least one tag is required.",
                new[] { nameof(Tags) });

            yield break;
        }

        foreach (var tag in Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tag values must contain at least one non-whitespace character.",
                    new[] { nameof(Tags) });

                continue;
            }

            var normalizedLength = tag.Trim().Length;
            if (normalizedLength is < 1 or > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag values must be between 1 and {AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });
            }
        }
    }
}