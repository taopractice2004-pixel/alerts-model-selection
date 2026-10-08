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
        string? tag = null,
        string sortBy = "createdDate",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<(int TotalCount, int ActiveCount, int InactiveCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the given normalized tag names to the alert, reusing existing Tag rows,
    /// and returns the updated alert (with its tags). Returns <c>null</c> if the alert is missing.
    /// </summary>
    Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> normalizedTagNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tag assignment (matched by normalized name) from the alert, deleting the
    /// Tag row if it becomes orphaned. Returns <c>false</c> if the alert or the assignment is missing.
    /// </summary>
    Task<bool> RemoveTagAsync(int alertId, string normalizedTagName, CancellationToken cancellationToken = default);
}
