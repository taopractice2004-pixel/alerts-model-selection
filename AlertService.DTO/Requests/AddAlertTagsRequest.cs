using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
        {
            yield break;
        }

        if (Tags.Count == 0)
        {
            yield return new ValidationResult(
                "At least one tag is required.",
                new[] { nameof(Tags) });
            yield break;
        }

        foreach (var tag in Tags)
        {
            var length = tag?.Trim().Length ?? 0;
            if (length < AlertConstants.TagMinLength || length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
