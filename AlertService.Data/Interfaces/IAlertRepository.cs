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

    /// <summary>
    /// Returns alert counts grouped by UTC creation day and severity for alerts created in
    /// [<paramref name="fromUtc"/>, <paramref name="toExclusiveUtc"/>). Groups without alerts are omitted.
    /// </summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetDailyCountsAsync(
        DateTime fromUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent active alert with the same title (case-insensitive) and severity
    /// created at or after <paramref name="createdSinceUtc"/>, or <c>null</c> if none exists.
    /// </summary>
    Task<Alert?> FindRecentActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdSinceUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tracked tags whose names match the given already-normalized names.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetTagsByNamesAsync(
        IReadOnlyCollection<string> normalizedNames,
        CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);
}
