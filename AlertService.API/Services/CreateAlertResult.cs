using AlertService.DTO.Responses;

namespace AlertService.API.Services;

/// <summary>
/// Outcome of <see cref="IAlertService.CreateAsync"/>, carrying whether the returned alert is a
/// newly created row or an existing alert matched by near-duplicate suppression.
/// </summary>
public record CreateAlertResult(AlertResponse Alert, bool IsDuplicate);
