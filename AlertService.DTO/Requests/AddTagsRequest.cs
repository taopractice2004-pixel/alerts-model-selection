using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(AlertConstants.MaxTagsPerAlert)]
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
        {
            yield break;
        }

        foreach (var tag in Tags)
        {
            var length = tag?.Trim().Length ?? 0;
            if (length < AlertConstants.TagMinLength || length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag must be between {AlertConstants.TagMinLength} and {AlertConstants.TagMaxLength} characters after trimming.",
                    new[] { nameof(Tags) });
                yield break;
            }
        }
    }
}
