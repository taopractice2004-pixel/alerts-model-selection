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
        bool? isActive,
        Severity? severity,
        DateTime? createdFrom,
        DateTime? createdTo,
        string? search,
        string sortBy,
        string sortDirection,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(
            isActive,
            severity,
            createdFrom,
            createdTo,
            search,
            sortBy,
            sortDirection,
            page,
            pageSize,
            tag: null,
            cancellationToken);
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
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(a => a.AlertTags.Any(at => at.Tag.Name.ToLower() == normalizedTag));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, sortBy, sortDirection);

        var items = await query
            .Include(a => a.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
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

    public async Task<IReadOnlyList<(DateTime DayUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailyTrendCountsAsync(
        DateTime startDateUtcInclusive,
        DateTime endDateUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var dailyCounts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startDateUtcInclusive && alert.CreatedDate < endDateUtcExclusive)
            .GroupBy(alert => alert.CreatedDate.Date)
            .Select(group => new
            {
                DayUtc = group.Key,
                TotalCount = group.Count(),
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .OrderBy(result => result.DayUtc)
            .ToListAsync(cancellationToken);

        return dailyCounts
            .Select(result =>
                (DateTime.SpecifyKind(result.DayUtc, DateTimeKind.Utc), result.TotalCount, result.LowCount, result.MediumCount, result.HighCount, result.CriticalCount))
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
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> FindActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdFromInclusive,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .Where(a =>
                a.IsActive
                && a.Severity == severity
                && a.CreatedDate >= createdFromInclusive
                && a.Title.ToLower() == normalizedTitle)
            .OrderByDescending(a => a.CreatedDate)
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

    public async Task<Alert?> AddTagsAsync(int id, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var normalizedTags = tags
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var normalizedTagsLower = normalizedTags.Select(value => value.ToLower()).ToList();

        var existingTagLookup = await _context.Tags
            .Where(tagEntity => normalizedTagsLower.Contains(tagEntity.Name.ToLower()))
            .ToDictionaryAsync(tagEntity => tagEntity.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var value in normalizedTags)
        {
            if (!existingTagLookup.TryGetValue(value, out var tagEntity))
            {
                tagEntity = new Tag { Name = value };
                _context.Tags.Add(tagEntity);
                existingTagLookup[value] = tagEntity;
            }

            var alreadyAssigned = alert.AlertTags.Any(alertTag => string.Equals(alertTag.Tag.Name, tagEntity.Name, StringComparison.OrdinalIgnoreCase));
            if (!alreadyAssigned)
            {
                alert.AlertTags.Add(new AlertTag
                {
                    Alert = alert,
                    Tag = tagEntity
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
    {
        var alertTag = await _context.AlertTags
            .Include(at => at.Tag)
            .FirstOrDefaultAsync(at => at.AlertId == id && at.Tag.Name.ToLower() == tag.ToLower(), cancellationToken);

        if (alertTag is null)
        {
            return false;
        }

        _context.AlertTags.Remove(alertTag);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
