using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var tag in Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tag names must not be empty or whitespace.",
                    new[] { nameof(Tags) });
            }
            else if (tag.Trim().Length > AlertConstants.TagNameMaxLength)
            {
                yield return new ValidationResult(
                    $"Tag names must be at most {AlertConstants.TagNameMaxLength} characters.",
                    new[] { nameof(Tags) });
            }
        }
    }
}
