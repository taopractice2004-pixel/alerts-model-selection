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

    Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Assigns the named tags to the alert, creating tags that do not exist yet. The alert must be loaded with its tags.</summary>
    Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag assignment (case-insensitive). The alert must be loaded with its tags.</summary>
    /// <returns><c>true</c> if the assignment existed and was removed.</returns>
    Task<bool> RemoveTagAsync(Alert alert, string tag, CancellationToken cancellationToken = default);

    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default);
}
