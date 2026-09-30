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
        string sortBy = AlertConstants.SortByCreatedDate,
        string sortDirection = AlertConstants.SortDirectionDesc,
        int page = AlertConstants.DefaultPageNumber,
        int pageSize = AlertConstants.DefaultPageSize,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag);

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
            query = query.Where(a => a.AlertTags.Any(at => at.Tag.Name.ToLower() == normalizedTag));
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

    public async Task<IReadOnlyList<(DateTime Day, Severity Severity, int Count)>> GetDailySeverityCountsAsync(
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startUtcInclusive && alert.CreatedDate < endUtcExclusive)
            .GroupBy(alert => new { Day = alert.CreatedDate.Date, alert.Severity })
            .Select(group => new
            {
                group.Key.Day,
                group.Key.Severity,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => (row.Day, row.Severity, row.Count))
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
        return _context.Alerts
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(alert.Id, cancellationToken) ?? alert;
    }

    public async Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        foreach (var tagName in tags)
        {
            if (alert.AlertTags.Any(at => string.Equals(at.Tag.Name, tagName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var normalizedTag = tagName.ToLower();
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedTag, cancellationToken);

            var tag = existingTag ?? new Tag { Name = tagName };
            if (existingTag is null)
            {
                await _context.Tags.AddAsync(tag, cancellationToken);
            }

            alert.AlertTags.Add(new AlertTag
            {
                AlertId = alert.Id,
                Tag = tag
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(alertId, cancellationToken);
    }

    public async Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        var existing = alert.AlertTags
            .FirstOrDefault(at => string.Equals(at.Tag.Name, tag, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            return false;
        }

        _context.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
