using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags.Count == 0)
        {
            yield return new ValidationResult(
                "At least one tag must be supplied.",
                new[] { nameof(Tags) });
            yield break;
        }

        foreach (var tag in Tags)
        {
            var trimmed = tag?.Trim() ?? string.Empty;
            if (trimmed.Length < AlertConstants.TagMinLength || trimmed.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be {AlertConstants.TagMinLength}-{AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
