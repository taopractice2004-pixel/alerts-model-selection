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

    /// <returns>Per-day, per-severity alert-creation counts for alerts created in
    /// [<paramref name="fromInclusiveUtc"/>, <paramref name="toExclusiveUtc"/>). Only non-empty
    /// day/severity groups are returned; callers fill missing days and severities with zero.</returns>
    Task<IReadOnlyList<(DateTime Day, Severity Severity, int Count)>> GetDailyCountsAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <returns>The most recent active alert with the same title (case-insensitive) and severity
    /// created on or after <paramref name="createdOnOrAfterUtc"/>, or <c>null</c> if none exists.</returns>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdOnOrAfterUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);
}
