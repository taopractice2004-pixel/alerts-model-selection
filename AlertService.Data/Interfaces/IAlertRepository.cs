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
    /// Returns raw, non-zero daily alert counts grouped by creation date (UTC calendar day) and
    /// severity, for alerts created within the inclusive range [<paramref name="fromDateUtc"/>,
    /// <paramref name="toDateUtc"/>]. Days or severities with no alerts are omitted; zero-filling
    /// is the caller's responsibility.
    /// </summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetTrendsAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the given tag names to the alert, reusing existing shared <c>Tag</c> rows via a
    /// case-insensitive name match and creating new rows only for names that do not already
    /// exist. Names already assigned to the alert (case-insensitively) are skipped.
    /// </summary>
    /// <returns>The updated alert with its tags loaded, or <c>null</c> if the alert does not exist.</returns>
    Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a single tag from the alert, matching <paramref name="tagName"/> case-insensitively.
    /// </summary>
    /// <returns><c>true</c> if a matching tag assignment was found and removed; <c>false</c> if the
    /// alert does not exist or has no such tag assigned.</returns>
    Task<bool> RemoveTagAsync(int alertId, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recent active alert matching <paramref name="title"/> (case-insensitive) and
    /// <paramref name="severity"/>, created on or after <paramref name="createdAfter"/>.
    /// </summary>
    /// <returns>The matching alert, or <c>null</c> if no such alert exists.</returns>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfter, CancellationToken cancellationToken = default);
}
