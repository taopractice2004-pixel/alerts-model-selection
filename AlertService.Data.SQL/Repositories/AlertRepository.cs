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
        IQueryable<Alert> query = _context.Alerts
            .AsNoTracking()
            .Include(a => a.Tags);

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
            query = query.Where(a => a.Tags.Any(t => t.NormalizedValue == normalizedTag));
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

    public async Task<IReadOnlyList<(DateTime DateUtc, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailySeverityCountsAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var grouped = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= fromUtcInclusive && alert.CreatedDate < toUtcExclusive)
            .GroupBy(alert => alert.CreatedDate.Date)
            .Select(group => new
            {
                DateUtc = group.Key,
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .OrderBy(group => group.DateUtc)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(group =>
                (
                    DateTime.SpecifyKind(group.DateUtc, DateTimeKind.Utc),
                    group.LowCount,
                    group.MediumCount,
                    group.HighCount,
                    group.CriticalCount
                ))
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

    public Task<Alert?> FindRecentActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdFromUtc,
        DateTime createdToUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var normalizedTitle = title.Trim().ToLowerInvariant();

        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.Tags)
            .Where(a => a.IsActive)
            .Where(a => a.Severity == severity)
            .Where(a => a.CreatedDate >= createdFromUtc && a.CreatedDate <= createdToUtc)
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

    public async Task<Alert?> AddTagsAsync(int alertId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var normalizedToInput = tags
            .Select(tag => new { Value = tag.Trim(), Normalized = NormalizeTag(tag) })
            .GroupBy(t => t.Normalized, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);

        var normalizedValues = normalizedToInput.Keys.ToArray();
        var existingTags = await _context.Tags
            .Where(t => normalizedValues.Contains(t.NormalizedValue))
            .ToDictionaryAsync(t => t.NormalizedValue, t => t, cancellationToken);

        foreach (var normalizedValue in normalizedValues)
        {
            var tag = existingTags.TryGetValue(normalizedValue, out var existingTag)
                ? existingTag
                : new Tag
                {
                    Value = normalizedToInput[normalizedValue],
                    NormalizedValue = normalizedValue
                };

            if (!existingTags.ContainsKey(normalizedValue))
            {
                _context.Tags.Add(tag);
                existingTags[normalizedValue] = tag;
            }

            if (alert.Tags.All(t => t.NormalizedValue != normalizedValue))
            {
                alert.Tags.Add(tag);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        var normalizedTag = NormalizeTag(tag);
        var tagToRemove = alert.Tags.FirstOrDefault(t => t.NormalizedValue == normalizedTag);
        if (tagToRemove is null)
        {
            return false;
        }

        alert.Tags.Remove(tagToRemove);
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

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }
}
