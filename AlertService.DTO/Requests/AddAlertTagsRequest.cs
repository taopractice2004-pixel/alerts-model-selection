using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null || Tags.Count == 0)
        {
            yield return new ValidationResult(
                "At least one tag is required.",
                new[] { nameof(Tags) });

            yield break;
        }

        foreach (var tag in Tags)
        {
            var trimmedTag = tag?.Trim() ?? string.Empty;

            if (trimmedTag.Length == 0)
            {
                yield return new ValidationResult(
                    "Tags must not contain blank values.",
                    new[] { nameof(Tags) });

                yield break;
            }

            if (trimmedTag.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between 1 and {AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });

                yield break;
            }
        }
    }
}