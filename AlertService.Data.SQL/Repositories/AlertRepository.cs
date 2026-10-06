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
            query = query.Where(a => a.Tags.Any(alertTag => alertTag.Name == tag));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, sortBy, sortDirection);

        var items = await query
            .Include(a => a.Tags)
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

    public async Task<IReadOnlyList<(DateTime Date, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailyTrendsAsync(
        DateTime startDateUtc,
        DateTime endDateExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var dailyBuckets = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startDateUtc && alert.CreatedDate < endDateExclusiveUtc)
            .GroupBy(alert => alert.CreatedDate.Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalCount = group.Count(),
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .OrderBy(bucket => bucket.Date)
            .ToListAsync(cancellationToken);

        return dailyBuckets
            .Select(bucket => (bucket.Date, bucket.TotalCount, bucket.LowCount, bucket.MediumCount, bucket.HighCount, bucket.CriticalCount))
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
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfterUtc, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.Tags)
            .Where(a => a.IsActive && a.Severity == severity && a.CreatedDate >= createdAfterUtc)
            .OrderByDescending(a => a.CreatedDate)
            .FirstOrDefaultAsync(a => a.Title.ToLower() == normalizedTitle, cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        if (tags.Count == 0)
        {
            return;
        }

        await _context.Entry(alert).Collection(a => a.Tags).LoadAsync(cancellationToken);

        var existingTags = await _context.Tags
            .Where(tag => tags.Contains(tag.Name))
            .ToListAsync(cancellationToken);

        var existingTagsByName = existingTags.ToDictionary(tag => tag.Name, StringComparer.Ordinal);
        var assignedTagNames = alert.Tags
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var tagName in tags)
        {
            if (assignedTagNames.Contains(tagName))
            {
                continue;
            }

            if (!existingTagsByName.TryGetValue(tagName, out var tagEntity))
            {
                tagEntity = new Tag
                {
                    Name = tagName
                };
                existingTagsByName[tagName] = tagEntity;
            }

            alert.Tags.Add(tagEntity);
            assignedTagNames.Add(tagName);
        }

        await _context.SaveChangesAsync(cancellationToken);
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

    public async Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.Tags)
            .ThenInclude(alertTag => alertTag.Alerts)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        var tagEntity = alert.Tags.FirstOrDefault(alertTag => alertTag.Name == tag);
        if (tagEntity is null)
        {
            return false;
        }

        var deleteTagEntity = tagEntity.Alerts.Count == 1;
        alert.Tags.Remove(tagEntity);

        if (deleteTagEntity)
        {
            _context.Tags.Remove(tagEntity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
