using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
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

        for (var index = 0; index < Tags.Count; index++)
        {
            var tag = Tags[index] ?? string.Empty;
            var normalized = tag.Trim();
            if (normalized.Length == 0 || normalized.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag at index {index} must be between 1 and {AlertConstants.TagMaxLength} characters.",
                    new[] { nameof(Tags) });
            }
        }
    }
}
