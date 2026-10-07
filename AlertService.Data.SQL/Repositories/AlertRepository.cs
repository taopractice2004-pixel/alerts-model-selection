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
        AlertQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var query = ApplyFilters(_context.Alerts.AsNoTracking(), options);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, options.SortBy, options.SortDirection);

        var items = await query
            .Include(a => a.Tags)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Alert> ApplyFilters(IQueryable<Alert> query, AlertQueryOptions options)
    {
        if (options.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == options.IsActive.Value);
        }

        if (options.Severity.HasValue)
        {
            query = query.Where(a => a.Severity == options.Severity.Value);
        }

        if (options.CreatedFrom.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= options.CreatedFrom.Value);
        }

        if (options.CreatedTo.HasValue)
        {
            query = query.Where(a => a.CreatedDate <= options.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var normalizedSearch = options.Search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(options.Tag))
        {
            var normalizedTag = Tag.NormalizeName(options.Tag);
            query = query.Where(a => a.Tags.Any(t => t.Name == normalizedTag));
        }

        return query;
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

    public async Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default)
    {
        var normalizedNames = tagNames.Select(Tag.NormalizeName).Distinct().ToList();

        var existingTags = await _context.Tags
            .Where(t => normalizedNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in normalizedNames)
        {
            var tag = existingTags.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name };
            if (!alert.Tags.Contains(tag))
            {
                alert.Tags.Add(tag);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveTagAsync(Alert alert, string tagName, CancellationToken cancellationToken = default)
    {
        var normalizedName = Tag.NormalizeName(tagName);
        var tag = alert.Tags.FirstOrDefault(t => t.Name == normalizedName);
        if (tag is null)
        {
            return false;
        }

        alert.Tags.Remove(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
