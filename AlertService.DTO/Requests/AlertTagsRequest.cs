using System.ComponentModel.DataAnnotations;

namespace AlertService.DTO.Requests;

public class AlertTagsRequest : IValidatableObject
{
    [Required]
    public ICollection<string>? Tags { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
        {
            yield break;
        }

        var normalizedTags = Tags.Select(tag => tag?.Trim() ?? string.Empty).ToList();
        if (normalizedTags.Count == 0 || normalizedTags.Any(tag => tag.Length is < 1 or > 30))
        {
            yield return new ValidationResult("Each tag must be between 1 and 30 characters.", new[] { nameof(Tags) });
        }

        if (normalizedTags.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 10)
        {
            yield return new ValidationResult("An alert cannot have more than 10 unique tags.", new[] { nameof(Tags) });
        }
    }
}