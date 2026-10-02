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
        string? tag = null,
        string? search = null,
        string sortBy = AlertConstants.SortByCreatedDate,
        string sortDirection = AlertConstants.SortDirectionDesc,
        int page = AlertConstants.DefaultPageNumber,
        int pageSize = AlertConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _context.Alerts
            .AsNoTracking()
            .Include(alert => alert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag);

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

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = NormalizeTag(tag);
            query = query.Where(alert => alert.AlertTags.Any(alertTag => alertTag.Tag.NormalizedName == normalizedTag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(normalizedSearch));
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

    public async Task<IReadOnlyList<(DateTime DayUtc, Severity Severity, int Count)>> GetDailySeverityCountsAsync(
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var groupedCounts = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= startUtcInclusive)
            .Where(alert => alert.CreatedDate < endUtcExclusive)
            .GroupBy(alert => new
            {
                DayUtc = alert.CreatedDate.Date,
                alert.Severity
            })
            .Select(group => new
            {
                group.Key.DayUtc,
                group.Key.Severity,
                Count = group.Count()
            })
            .OrderBy(item => item.DayUtc)
            .ThenBy(item => item.Severity)
            .ToListAsync(cancellationToken);

        return groupedCounts
            .Select(item => (item.DayUtc, item.Severity, item.Count))
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
            .Include(alert => alert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Alert?> FindRecentActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdAfterUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLowerInvariant();

        return _context.Alerts
            .AsNoTracking()
            .Include(alert => alert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .Where(alert => alert.IsActive)
            .Where(alert => alert.Severity == severity)
            .Where(alert => alert.CreatedDate >= createdAfterUtc)
            .Where(alert => alert.Title.ToLower() == normalizedTitle)
            .OrderByDescending(alert => alert.CreatedDate)
            .ThenByDescending(alert => alert.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(existingAlert => existingAlert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var requestedTags = tags
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedTags.Count == 0)
        {
            return alert;
        }

        var requestedNormalizedTags = requestedTags
            .Select(NormalizeTag)
            .ToHashSet(StringComparer.Ordinal);

        var assignedNormalizedTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.NormalizedName)
            .ToHashSet(StringComparer.Ordinal);

        var normalizedTagsToAdd = requestedNormalizedTags
            .Where(normalizedTag => !assignedNormalizedTags.Contains(normalizedTag))
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedTagsToAdd.Count == 0)
        {
            return alert;
        }

        var existingTags = await _context.Tags
            .Where(tagEntity => normalizedTagsToAdd.Contains(tagEntity.NormalizedName))
            .ToListAsync(cancellationToken);

        var tagsByNormalizedName = existingTags.ToDictionary(tagEntity => tagEntity.NormalizedName, StringComparer.Ordinal);

        foreach (var requestedTag in requestedTags)
        {
            var normalizedTag = NormalizeTag(requestedTag);
            if (!normalizedTagsToAdd.Contains(normalizedTag))
            {
                continue;
            }

            if (!tagsByNormalizedName.TryGetValue(normalizedTag, out var tagEntity))
            {
                tagEntity = new Tag
                {
                    Name = requestedTag,
                    NormalizedName = normalizedTag
                };

                _context.Tags.Add(tagEntity);
                tagsByNormalizedName[normalizedTag] = tagEntity;
            }

            alert.AlertTags.Add(new AlertTag
            {
                Alert = alert,
                Tag = tagEntity
            });

            normalizedTagsToAdd.Remove(normalizedTag);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<Alert?> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(existingAlert => existingAlert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var normalizedTag = NormalizeTag(tag);
        var alertTag = alert.AlertTags
            .FirstOrDefault(existingAlertTag => existingAlertTag.Tag.NormalizedName == normalizedTag);

        if (alertTag is null)
        {
            return null;
        }

        alert.AlertTags.Remove(alertTag);
        _context.AlertTags.Remove(alertTag);
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

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }
}
