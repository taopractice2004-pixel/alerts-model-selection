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

    /// <summary>Gets alert counts grouped by UTC creation day and severity for alerts created in [<paramref name="startInclusive"/>, <paramref name="endExclusive"/>). Only non-empty groups are returned.</summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetDailySeverityCountsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets the most recent active alert with the same title (case-insensitive) and severity created at or after <paramref name="createdSince"/>.</summary>
    Task<Alert?> FindRecentActiveDuplicateAsync(string title, Severity severity, DateTime createdSince, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>Attaches the named tags to the alert, reusing existing tags matched case-insensitively.</summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    Task RemoveTagAsync(Alert alert, Tag tag, CancellationToken cancellationToken = default);
}
