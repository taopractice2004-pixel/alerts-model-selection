using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AssignAlertTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags.Count == 0)
        {
            yield return new ValidationResult("At least one tag is required.", new[] { nameof(Tags) });
            yield break;
        }

        foreach (var tag in Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tags must contain at least one non-whitespace character.",
                    new[] { nameof(Tags) });
                continue;
            }

            if (tag.Trim().Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tags must be {AlertConstants.TagMaxLength} characters or fewer.",
                    new[] { nameof(Tags) });
            }
        }
    }
}