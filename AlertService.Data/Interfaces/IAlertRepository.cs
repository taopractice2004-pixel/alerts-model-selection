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

    /// <summary>Counts alerts by UTC creation date and severity for <paramref name="fromUtc"/> (inclusive) to <paramref name="toUtc"/> (exclusive); groups with no alerts are omitted.</summary>
    Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetDailySeverityCountsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Finds the most recent active alert with the same severity and a case-insensitively equal title created at or after <paramref name="createdFromUtc"/>.</summary>
    Task<Alert?> FindRecentActiveDuplicateAsync(string title, Severity severity, DateTime createdFromUtc, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>Assigns the tags (already normalized) to an alert loaded by <see cref="GetByIdAsync"/>, creating missing tags.</summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> normalizedNames, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the tag was assigned to the alert and removed; otherwise <c>false</c>.</returns>
    Task<bool> RemoveTagAsync(Alert alert, string normalizedName, CancellationToken cancellationToken = default);
}
