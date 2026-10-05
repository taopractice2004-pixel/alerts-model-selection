using System.ComponentModel.DataAnnotations;
using AlertService.Common.Constants;
using AlertService.Common.Enums;

namespace AlertService.DTO.Requests;

public class AlertQueryRequest : IValidatableObject
{
    public bool? IsActive { get; set; }

    [EnumDataType(typeof(Severity))]
    public Severity? Severity { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    [Range(AlertConstants.DefaultPageNumber, int.MaxValue)]
    public int Page { get; set; } = AlertConstants.DefaultPageNumber;

    [Range(1, AlertConstants.MaxPageSize)]
    public int PageSize { get; set; } = AlertConstants.DefaultPageSize;

    [RegularExpression(AlertConstants.SortByPattern)]
    public string SortBy { get; set; } = AlertConstants.SortByCreatedDate;

    [RegularExpression(AlertConstants.SortDirectionPattern)]
    public string SortDirection { get; set; } = AlertConstants.SortDirectionDesc;

    [StringLength(AlertConstants.SearchMaxLength)]
    public string? Search { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom.HasValue && CreatedTo.HasValue && CreatedFrom.Value > CreatedTo.Value)
        {
            yield return new ValidationResult(
                "CreatedFrom must be less than or equal to CreatedTo.",
                new[] { nameof(CreatedFrom), nameof(CreatedTo) });
        }
    }
}