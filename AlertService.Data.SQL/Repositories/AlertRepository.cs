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
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(a => a.AlertTags.Any(alertTag => alertTag.Tag.Name.ToLower() == normalizedTag));
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

    public async Task<IReadOnlyList<AlertTrendCount>> GetDailyTrendsAsync(
        DateTime startDateUtc,
        DateTime endDateUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var trendCounts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startDateUtc && alert.CreatedDate < endDateUtcExclusive)
            .GroupBy(alert => alert.CreatedDate.Date)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Date = group.Key,
                TotalCount = group.Count(),
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .ToListAsync(cancellationToken);

        return trendCounts
            .Select(trendCount => new AlertTrendCount(
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
            .Include(a => a.AlertTags)
                .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> GetActiveDuplicateAsync(string title, Severity severity, DateTime createdAfterUtc, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return _context.Alerts
            .Include(a => a.AlertTags)
                .ThenInclude(alertTag => alertTag.Tag)
            .Where(a => a.IsActive
                && a.Severity == severity
                && a.CreatedDate >= createdAfterUtc
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

    public async Task AddTagsAsync(Alert alert, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var requestedTags = tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!requestedTags.Any())
        {
            return;
        }

        var normalizedRequestedTags = requestedTags
            .Select(requestedTag => requestedTag.ToLower())
            .ToList();

        var existingTags = await _context.Tags
            .Where(tag => normalizedRequestedTags.Contains(tag.Name.ToLower()))
            .ToListAsync(cancellationToken);

        var tagsByName = existingTags.ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
        var assignedTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var requestedTag in requestedTags)
        {
            if (assignedTags.Contains(requestedTag))
            {
                continue;
            }

            if (!tagsByName.TryGetValue(requestedTag, out var tag))
            {
                tag = new Tag
                {
                    Name = requestedTag
                };

                await _context.Tags.AddAsync(tag, cancellationToken);
                tagsByName[requestedTag] = tag;
            }

            alert.AlertTags.Add(new AlertTag
            {
                Alert = alert,
                Tag = tag
            });

            assignedTags.Add(requestedTag);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveTagAsync(Alert alert, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = tag.Trim();
        var alertTag = alert.AlertTags
            .First(alertTag => string.Equals(alertTag.Tag.Name, normalizedTag, StringComparison.OrdinalIgnoreCase));

        _context.AlertTags.Remove(alertTag);
        alert.AlertTags.Remove(alertTag);
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
}
