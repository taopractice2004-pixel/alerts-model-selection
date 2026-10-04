using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    public List<string?> Tags { get; set; } = [];

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
            var trimmedTag = Tags[index]?.Trim() ?? string.Empty;
            if (trimmedTag.Length == 0 || trimmedTag.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag at index {index} must be between 1 and {AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });
            }
        }
    }
}