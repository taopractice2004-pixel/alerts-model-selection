using AlertService.Models;
using AlertService.Common.Enums;

namespace AlertService.Data.Interfaces;

/// <summary>
/// Data access contract for alerts. Implementations live in provider-specific
/// projects (e.g. AlertService.Data.SQL) so the service layer never depends on EF Core.
/// </summary>
public interface IAlertRepository
{
    Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetAllAsync(
        bool? isActive = null,
        Severity? severity = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        string? search = null,
        string sortBy = "createdDate",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        CancellationToken cancellationToken = default);

    Task<(int TotalCount, int ActiveCount, int InactiveCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>Adds the given tag names to an alert, reusing existing tag rows (case-insensitive).</summary>
    /// <returns>The updated alert, or <c>null</c> if no alert with the given id exists.</returns>
    Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the tag assignment was removed, <c>false</c> if the alert or assignment was not found.</returns>
    Task<bool> RemoveTagAsync(int alertId, string tagName, CancellationToken cancellationToken = default);
}