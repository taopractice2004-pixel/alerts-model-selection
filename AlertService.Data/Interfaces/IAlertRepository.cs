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

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most recent active alert with the same title (case-insensitive) and severity
    /// created at or after <paramref name="createdFromUtc"/>, or <c>null</c> if none exists.
    /// </summary>
    Task<Alert?> FindRecentActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdFromUtc,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the given tag names to a tracked alert, creating any tags that do not exist yet.
    /// Names must already be trimmed, validated and not present on the alert.
    /// </summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>Detaches a tag from a tracked alert.</summary>
    Task RemoveTagAsync(Alert alert, Tag tag, CancellationToken cancellationToken = default);
}
