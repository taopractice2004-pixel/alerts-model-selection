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
    /// Returns server-side grouped alert-creation counts per UTC calendar day and severity for
    /// alerts created in the half-open range [<paramref name="fromInclusive"/>, <paramref name="toExclusive"/>).
    /// Days and severities with no alerts are simply absent; zero-fill is the caller's responsibility.
    /// </summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetDailyCountsBySeverityAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recent active alert with the same <paramref name="severity"/> and a title
    /// equal to <paramref name="title"/> (case-insensitive) created at or after
    /// <paramref name="createdAfter"/>. Used to suppress near-duplicate alerts on create.
    /// </summary>
    /// <returns>The matching active alert, or <c>null</c> when none qualifies.</returns>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfter, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds or reuses a <see cref="Tag"/> row (case-insensitive) for each supplied name and
    /// links it to the alert. Names already linked to the alert are ignored.
    /// </summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the tag assignment matching <paramref name="tagName"/> (case-insensitive) from the alert.
    /// </summary>
    /// <returns><c>true</c> if an assignment was removed; <c>false</c> if the alert had no such tag.</returns>
    Task<bool> RemoveTagAsync(Alert alert, string tagName, CancellationToken cancellationToken = default);
}
