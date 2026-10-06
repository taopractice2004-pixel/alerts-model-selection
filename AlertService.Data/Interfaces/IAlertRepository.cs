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

    /// <summary>
    /// Returns alert-creation counts grouped by UTC calendar day and severity for alerts created
    /// in the half-open window [<paramref name="fromUtcInclusive"/>, <paramref name="toUtcExclusive"/>).
    /// Only day/severity combinations with at least one alert are returned; zero-fill is applied by the caller.
    /// </summary>
    Task<IReadOnlyList<(DateTime Day, Severity Severity, int Count)>> GetDailySeverityCountsAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recently created active alert with a matching title (case-insensitive)
    /// and severity created at or after <paramref name="createdAfterUtc"/>, used for
    /// near-duplicate suppression. Returns <c>null</c> when no such alert exists.
    /// </summary>
    Task<Alert?> FindRecentDuplicateAsync(string title, Severity severity, DateTime createdAfterUtc, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links the given tag names to the alert, reusing existing global tags (matched
    /// case-insensitively) and creating any that do not yet exist.
    /// </summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the tag was assigned and removed, <c>false</c> if it was not assigned.</returns>
    Task<bool> RemoveTagAsync(Alert alert, string tagName, CancellationToken cancellationToken = default);
}
