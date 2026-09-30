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
        IReadOnlyCollection<string>? tags = null,
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

        if (tags is { Count: > 0 })
        {
            var normalizedTags = tags.Select(tag => tag.ToLower()).ToArray();
            query = query.Where(alert => alert.Tags.Any(tag => normalizedTags.Contains(tag.Name.ToLower())));
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
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var groupedCounts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startDate && alert.CreatedDate < endDate)
            .GroupBy(alert => new { Date = alert.CreatedDate.Date, alert.Severity })
            .Select(group => new
            {
                group.Key.Date,
                group.Key.Severity,
                Count = group.Count()
            })
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Severity)
            .ToListAsync(cancellationToken);

        return groupedCounts
            .Select(item => (item.Date, item.Severity, item.Count))
            .ToList();
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
        return _context.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> GetActiveNearDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdFrom,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.ToLower();
        return _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.IsActive
                && alert.Severity == severity
                && alert.CreatedDate >= createdFrom
                && alert.Title.ToLower() == normalizedTitle)
            .OrderByDescending(alert => alert.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts.Include(a => a.Tags).FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert is null)
        {
            return null;
        }

        var existingTags = await _context.Tags.ToListAsync(cancellationToken);
        foreach (var tagName in tags)
        {
            var tag = existingTags.FirstOrDefault(existing => string.Equals(existing.Name, tagName, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                existingTags.Add(tag);
            }

            if (!alert.Tags.Any(existing => (tag.Id != 0 && existing.Id == tag.Id)
                || string.Equals(existing.Name, tag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                alert.Tags.Add(tag);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts.Include(a => a.Tags).FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        var assignedTag = alert?.Tags.FirstOrDefault(existing => string.Equals(existing.Name, tag, StringComparison.OrdinalIgnoreCase));
        if (assignedTag is null)
        {
            return false;
        }

        alert!.Tags.Remove(assignedTag);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
}
