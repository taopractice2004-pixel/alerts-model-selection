using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>Result of creating an alert; <see cref="IsDuplicate"/> is true when an existing alert was returned instead of inserting.</summary>
public sealed record CreateAlertResult(AlertResponse Alert, bool IsDuplicate);
