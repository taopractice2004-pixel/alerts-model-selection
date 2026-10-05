using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < Tags.Count; index++)
        {
            var tag = Tags[index];

            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tag values must include at least one non-whitespace character.",
                    [nameof(Tags)]);
                continue;
            }

            var trimmedLength = tag.Trim().Length;
            if (trimmedLength is < 1 or > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult(
                    $"Each tag length must be between 1 and {AlertConstants.TagMaxLength} characters after trimming.",
                    [nameof(Tags)]);
            }
        }
    }
}