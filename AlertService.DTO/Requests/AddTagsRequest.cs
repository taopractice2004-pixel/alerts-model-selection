using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

/// <summary>
/// Request to attach one or more free-form tags to an alert. Tags are trimmed and
/// deduplicated case-insensitively by the service; the per-alert maximum is enforced there.
/// </summary>
public class AddTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = new();

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
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
