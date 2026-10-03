using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AddAlertTagsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public string[] Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < Tags.Length; index++)
        {
            var tag = Tags[index];
            if (string.IsNullOrWhiteSpace(tag))
            {
                yield return new ValidationResult(
                    "Tag cannot be empty or whitespace.",
                    new[] { $"{nameof(Tags)}[{index}]" });
                continue;
            }

            var trimmed = tag.Trim();
            if (trimmed.Length is < 1 or > 30)
            {
                yield return new ValidationResult(
                    "Each tag must be between 1 and 30 characters.",
                    new[] { $"{nameof(Tags)}[{index}]" });
            }
        }
    }
}
