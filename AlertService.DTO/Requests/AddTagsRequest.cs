using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags.Count == 0)
        {
            yield return new ValidationResult("At least one tag is required.", new[] { nameof(Tags) });
            yield break;
        }

        if (Tags.Count > AlertConstants.MaxTagsPerAlert)
        {
            yield return new ValidationResult(
                $"A single request cannot contain more than {AlertConstants.MaxTagsPerAlert} tags.",
                new[] { nameof(Tags) });
            yield break;
        }

        foreach (var tag in Tags)
        {
            var trimmed = tag?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length < AlertConstants.TagMinLength || trimmed.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be {AlertConstants.TagMinLength}-{AlertConstants.TagMaxLength} characters.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
