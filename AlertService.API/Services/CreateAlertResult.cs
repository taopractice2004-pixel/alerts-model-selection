using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>Outcome of creating an alert; <see cref="DuplicateSuppressed"/> is true when an existing alert was returned instead.</summary>
public sealed record CreateAlertResult(AlertResponse Alert, bool DuplicateSuppressed);
