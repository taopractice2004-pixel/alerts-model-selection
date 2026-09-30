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
        IQueryable<Alert> query = _context.Alerts.AsNoTracking().Include(a => a.Tags);

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
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(a => a.Tags.Any(t => t.Name.ToLower() == normalizedTag));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, sortBy, sortDirection);

        var items = await query
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

    public async Task<IReadOnlyList<(DateTime Date, Severity Severity, int Count)>> GetTrendCountsAsync(
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var counts = await _context.Alerts
            .AsNoTracking()
            .Where(a => a.CreatedDate >= startInclusiveUtc && a.CreatedDate < endExclusiveUtc)
            .GroupBy(a => new { Date = a.CreatedDate.Date, a.Severity })
            .Select(g => new { g.Key.Date, g.Key.Severity, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.Select(c => (c.Date, c.Severity, c.Count)).ToList();
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
        return _context.Alerts.Include(a => a.Tags).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> FindRecentActiveDuplicateAsync(string title, Severity severity, DateTime createdFromUtc, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.ToLower();

        return _context.Alerts
            .Where(a => a.IsActive
                && a.Severity == severity
                && a.CreatedDate >= createdFromUtc
                && a.Title.ToLower() == normalizedTitle)
            .OrderByDescending(a => a.CreatedDate)
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetOrCreateTagsAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedNames.Count == 0)
        {
            return Array.Empty<Tag>();
        }

        var lowerNames = normalizedNames.Select(n => n.ToLower()).ToList();
        var existingTags = await _context.Tags
            .Where(t => lowerNames.Contains(t.Name.ToLower()))
            .ToListAsync(cancellationToken);

        var result = new List<Tag>(existingTags);
        foreach (var name in normalizedNames)
        {
            if (!existingTags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                var newTag = new Tag { Name = name };
                _context.Tags.Add(newTag);
                result.Add(newTag);
            }
        }

        return result;
    }
}
