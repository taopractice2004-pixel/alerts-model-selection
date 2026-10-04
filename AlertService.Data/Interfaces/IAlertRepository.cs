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
    /// in the half-open range [<paramref name="fromUtcInclusive"/>, <paramref name="toUtcExclusive"/>).
    /// Days or severities with no alerts are simply absent; zero-filling is the caller's responsibility.
    /// </summary>
    Task<IReadOnlyList<(DateTime DayUtc, Severity Severity, int Count)>> GetDailySeverityCountsAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent active alert matching the given title (case-insensitive) and
    /// severity that was created at or after <paramref name="createdAfterUtc"/>, or <c>null</c>
    /// if none exists. Used to detect near-duplicate alerts within a suppression window.
    /// </summary>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfterUtc, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the given tag names to the alert, reusing existing tag rows (matched
    /// case-insensitively) and creating new ones as needed.
    /// </summary>
    Task AddTagsToAlertAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag association from the alert (the shared tag row is retained).</summary>
    Task RemoveTagFromAlertAsync(Alert alert, Tag tag, CancellationToken cancellationToken = default);
}
