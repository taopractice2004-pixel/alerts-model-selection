using AlertService.Data.Interfaces;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertService.Data.SQL.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly AlertDbContext _context;

    public AlertRepository(AlertDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetAllAsync(
        bool? isActive = null,
        Severity? severity = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        string? search = null,
        string? tag = null,
        string sortBy = AlertConstants.SortByCreatedDate,
        string sortDirection = AlertConstants.SortDirectionDesc,
        int page = AlertConstants.DefaultPageNumber,
        int pageSize = AlertConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _context.Alerts.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(a => a.Severity == severity.Value);
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(a => a.CreatedDate <= createdTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = NormalizeTag(tag);
            query = query.Where(a => a.Tags.Any(t => t.NormalizedName == normalizedTag));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sortedQuery = ApplySorting(query, sortBy, sortDirection)
            .Include(a => a.Tags);

        var items = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int TotalCount, int ActiveCount, int InactiveCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await _context.Alerts
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                ActiveCount = group.Count(alert => alert.IsActive),
                InactiveCount = group.Count(alert => !alert.IsActive),
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return summary is null
            ? (0, 0, 0, 0, 0, 0, 0)
            : (summary.TotalCount, summary.ActiveCount, summary.InactiveCount, summary.LowCount, summary.MediumCount, summary.HighCount, summary.CriticalCount);
    }

    public async Task<IReadOnlyList<(DateOnly DateUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailyTrendsAsync(
        DateOnly startDateUtc,
        DateOnly endDateUtc,
        CancellationToken cancellationToken = default)
    {
        if (endDateUtc < startDateUtc)
        {
            return Array.Empty<(DateOnly DateUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>();
        }

        var startUtc = startDateUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusiveUtc = endDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var matchingAlerts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startUtc && alert.CreatedDate < endExclusiveUtc)
            .Select(alert => new { alert.CreatedDate, alert.Severity })
            .ToListAsync(cancellationToken);

        var countsByDate = matchingAlerts
            .GroupBy(alert => DateOnly.FromDateTime(DateTime.SpecifyKind(alert.CreatedDate, DateTimeKind.Utc).Date))
            .ToDictionary(
                group => group.Key,
                group => (
                    TotalCount: group.Count(),
                    LowCount: group.Count(alert => alert.Severity == Severity.Low),
                    MediumCount: group.Count(alert => alert.Severity == Severity.Medium),
                    HighCount: group.Count(alert => alert.Severity == Severity.High),
                    CriticalCount: group.Count(alert => alert.Severity == Severity.Critical)));

        var buckets = new List<(DateOnly DateUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>();
        for (var date = startDateUtc; date <= endDateUtc; date = date.AddDays(1))
        {
            var counts = countsByDate.GetValueOrDefault(date);
            buckets.Add((
                date,
                counts.TotalCount,
                counts.LowCount,
                counts.MediumCount,
                counts.HighCount,
                counts.CriticalCount));
        }

        return buckets;
    }

    private static IQueryable<Alert> ApplySorting(IQueryable<Alert> query, string sortBy, string sortDirection)
    {
        var normalizedSortBy = sortBy.Trim();
        var descending = string.Equals(sortDirection, AlertConstants.SortDirectionDesc, StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy.ToLowerInvariant() switch
        {
            AlertConstants.SortBySeverity => descending
                ? query.OrderByDescending(a => a.Severity == Severity.Critical
                    ? 4
                    : a.Severity == Severity.High
                        ? 3
                        : a.Severity == Severity.Medium
                            ? 2
                            : 1)
                    .ThenByDescending(a => a.CreatedDate)
                : query.OrderBy(a => a.Severity == Severity.Critical
                    ? 4
                    : a.Severity == Severity.High
                        ? 3
                        : a.Severity == Severity.Medium
                            ? 2
                            : 1)
                    .ThenByDescending(a => a.CreatedDate),
            AlertConstants.SortByTitle => descending
                ? query.OrderByDescending(a => a.Title).ThenByDescending(a => a.CreatedDate)
                : query.OrderBy(a => a.Title).ThenByDescending(a => a.CreatedDate),
            _ => descending
                ? query.OrderByDescending(a => a.CreatedDate)
                : query.OrderBy(a => a.CreatedDate)
        };
    }

    public Task<Alert?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Alerts
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdSinceUtc, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return _context.Alerts
            .Include(a => a.Tags)
            .Where(a => a.IsActive)
            .Where(a => a.Severity == severity)
            .Where(a => a.CreatedDate >= createdSinceUtc)
            .Where(a => a.Title.ToLower() == normalizedTitle)
            .OrderByDescending(a => a.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task AssignTagsAsync(Alert alert, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        if (tags.Count == 0)
        {
            return;
        }

        var tagsByNormalizedName = tags
            .GroupBy(NormalizeTag)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var existingTags = await _context.Tags
            .Where(tag => tagsByNormalizedName.Keys.Contains(tag.NormalizedName))
            .ToListAsync(cancellationToken);

        var existingNormalizedNames = existingTags
            .Select(tag => tag.NormalizedName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var existingTag in existingTags)
        {
            if (alert.Tags.All(tag => !string.Equals(tag.NormalizedName, existingTag.NormalizedName, StringComparison.Ordinal)))
            {
                alert.Tags.Add(existingTag);
            }
        }

        foreach (var (normalizedName, displayName) in tagsByNormalizedName)
        {
            if (existingNormalizedNames.Contains(normalizedName))
            {
                continue;
            }

            alert.Tags.Add(new Tag
            {
                Name = displayName,
                NormalizedName = normalizedName
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(alert).State == EntityState.Detached)
        {
            _context.Alerts.Update(alert);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeTag(string tag) => tag.Trim().ToUpperInvariant();
}
