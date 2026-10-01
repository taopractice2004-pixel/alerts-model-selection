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
    /// Returns per-UTC-day severity counts for alerts created within the window
    /// <c>[fromInclusive, toExclusive)</c> on <see cref="Alert.CreatedDate"/>. Counts cover all
    /// alerts regardless of <see cref="Alert.IsActive"/>. Only days and severities with at least
    /// one alert are returned; callers fill zero-count days and severities.
    /// </summary>
    Task<IReadOnlyList<(DateTime Day, Severity Severity, int Count)>> GetDailyTrendsAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);


    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recent ACTIVE alert that matches the given title (case-insensitive, trimmed)
    /// and severity, created on or after the supplied UTC cutoff. Used to suppress near-duplicate
    /// alert creation.
    /// </summary>
    /// <returns>The most recent matching active alert, or <c>null</c> if none exists.</returns>
    Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdOnOrAfterUtc, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the given canonical tag names to the alert, reusing existing <see cref="Tag"/>
    /// rows that match case-insensitively and skipping any already assigned.
    /// </summary>
    Task AddTagsAsync(int alertId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tag assignment from the alert (case-insensitive match).
    /// </summary>
    /// <returns><c>true</c> if removed; <c>false</c> if the alert or the assignment was missing.</returns>
    Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default);
}
