using AlertService.Common.Constants;
using AlertService.Common.Enums;

namespace AlertService.Data.Interfaces;

public sealed class AlertQueryOptions
{
    public bool? IsActive { get; init; }

    public Severity? Severity { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }

    public string? Search { get; init; }

    public string? Tag { get; init; }

    public string SortBy { get; init; } = AlertConstants.SortByCreatedDate;

    public string SortDirection { get; init; } = AlertConstants.SortDirectionDesc;

    public int Page { get; init; } = AlertConstants.DefaultPageNumber;

    public int PageSize { get; init; } = AlertConstants.DefaultPageSize;
}