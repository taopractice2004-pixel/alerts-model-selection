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
    /// Returns per-day, per-severity alert creation counts for <see cref="Alert.CreatedDate"/>
    /// values in [<paramref name="startInclusiveUtc"/>, <paramref name="endExclusiveUtc"/>).
    /// Days/severities with no alerts are omitted (callers zero-fill).
    /// </summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetTrendCountsAsync(
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recently created active alert with the given title (case-insensitive) and
    /// severity, created on or after <paramref name="createdFromUtc"/>. Used to suppress
    /// near-duplicate alert creation.
    /// </summary>
    Task<Alert?> FindRecentActiveDuplicateAsync(string title, Severity severity, DateTime createdFromUtc, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves each name to an existing tracked <see cref="Tag"/> (matched case-insensitively) or
    /// creates a new, not-yet-saved one. Callers must save changes (e.g. via <see cref="UpdateAsync"/>).
    /// </summary>
    Task<IReadOnlyList<Tag>> GetOrCreateTagsAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
}
