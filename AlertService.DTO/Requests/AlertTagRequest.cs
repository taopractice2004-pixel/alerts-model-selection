using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;

namespace AlertService.DTO.Requests;

public class AlertTagRequest : IValidatableObject
{
    public List<string> Tags { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null || Tags.Count == 0)
        {
            yield return new ValidationResult("Tags must contain at least one value.", new[] { nameof(Tags) });
            yield break;
        }

        if (Tags.Count > AlertConstants.MaxTagsPerAlert)
        {
            yield return new ValidationResult($"A maximum of {AlertConstants.MaxTagsPerAlert} tags is allowed.", new[] { nameof(Tags) });
        }

        for (var i = 0; i < Tags.Count; i++)
        {
            var t = Tags[i]?.Trim();
            if (string.IsNullOrEmpty(t))
            {
                yield return new ValidationResult("Tag must contain non-whitespace characters.", new[] { $"Tags[{i}]" });
                continue;
            }

            if (t.Length < 1 || t.Length > AlertConstants.TagMaxLength)
            {
                yield return new ValidationResult($"Tag must be between 1 and {AlertConstants.TagMaxLength} characters.", new[] { $"Tags[{i}]" });
            }
        }
    }
}
