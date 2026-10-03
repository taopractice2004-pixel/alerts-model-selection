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
            var normalizedTag = tag.Trim().ToUpperInvariant();
            query = query.Where(a => a.Tags.Any(t => t.Name.ToUpper() == normalizedTag));
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

    public async Task<IReadOnlyList<(DateTime Date, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetTrendCountsAsync(
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var trendCounts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startUtcInclusive)
            .Where(alert => alert.CreatedDate < endUtcExclusive)
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
            .OrderBy(trendCount => trendCount.Date)
            .ToListAsync(cancellationToken);

        return trendCounts
            .Select(trendCount => (
                DateTime.SpecifyKind(trendCount.Date, DateTimeKind.Utc),
                trendCount.TotalCount,
                trendCount.LowCount,
                trendCount.MediumCount,
                trendCount.HighCount,
                trendCount.CriticalCount))
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

    public Task<Alert?> FindActiveDuplicateAsync(
        string normalizedTitle,
        Severity severity,
        DateTime createdAfterUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedTitle);

        var comparisonTitle = normalizedTitle.Trim().ToUpperInvariant();

        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.Tags)
            .Where(a => a.IsActive)
            .Where(a => a.Severity == severity)
            .Where(a => a.CreatedDate >= createdAfterUtc)
            .Where(a => a.Title.ToUpper() == comparisonTitle)
            .OrderByDescending(a => a.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<Alert> AddTagsAsync(Alert alert, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(tagNames);

        if (tagNames.Count == 0)
        {
            return alert;
        }

        var normalizedTagNames = tagNames
            .Select(tagName => tagName.Trim())
            .Where(tagName => tagName.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTagNames.Count == 0)
        {
            return alert;
        }

        var existingTags = await LoadExistingTagsByNameAsync(normalizedTagNames, cancellationToken);
        var assignedTagNames = alert.Tags
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tagName in normalizedTagNames)
        {
            if (assignedTagNames.Contains(tagName))
            {
                continue;
            }

            var tag = existingTags.FirstOrDefault(existingTag => string.Equals(existingTag.Name, tagName, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                tag = new Tag
                {
                    Name = tagName
                };

                _context.Tags.Add(tag);
                existingTags.Add(tag);
            }

            alert.Tags.Add(tag);
            assignedTagNames.Add(tagName);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task RemoveTagAsync(Alert alert, string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var assignedTag = alert.Tags.First(existingTag => string.Equals(existingTag.Name, tag, StringComparison.OrdinalIgnoreCase));
        alert.Tags.Remove(assignedTag);

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

    private Task<List<Tag>> LoadExistingTagsByNameAsync(IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken)
    {
        var normalizedTagNames = tagNames
            .Select(tagName => tagName.ToUpperInvariant())
            .ToList();

        return _context.Tags
            .Where(tag => normalizedTagNames.Contains(tag.Name.ToUpper()))
            .ToListAsync(cancellationToken);
    }
}
