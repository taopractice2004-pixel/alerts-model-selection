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
    /// Returns per-UTC-calendar-day severity counts for alerts created within
    /// <c>[fromInclusiveUtc, toExclusiveUtc)</c>. Only days that have alerts are returned;
    /// zero-filling of missing days and severities is the caller's responsibility.
    /// </summary>
    Task<IReadOnlyList<(DateTime Day, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailyTrendsAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recent active alert with the same severity and (case-insensitive) title
    /// created on or after <paramref name="createdOnOrAfter"/>, or <c>null</c> if none exists.
    /// </summary>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdOnOrAfter, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the given tag names to the alert, reusing existing <see cref="Tag"/> rows
    /// (case-insensitive) and creating new ones as needed, then persists the changes.
    /// </summary>
    Task AddTagsToAlertAsync(Alert alert, IReadOnlyCollection<string> newTagNames, CancellationToken cancellationToken = default);
}
