using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    [Required]
    public IReadOnlyList<string> Tags { get; set; } = new List<string>();

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
            var trimmed = tag?.Trim() ?? string.Empty;
            if (trimmed.Length < AlertConstants.TagMinLength || trimmed.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
