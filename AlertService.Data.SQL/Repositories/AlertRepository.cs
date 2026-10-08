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

        var filteredQuery = ApplyFilters(CreateAlertsQuery(), options);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var sortedQuery = ApplySorting(filteredQuery, options.SortBy, options.SortDirection);

        var items = await ApplyPaging(sortedQuery, options.Page, options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<Alert> CreateAlertsQuery()
    {
        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag);
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

        query = ApplySearchFilter(query, options.Search);
        return ApplyTagFilter(query, options.Tag);
    }

    private static IQueryable<Alert> ApplySearchFilter(IQueryable<Alert> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalizedSearch = search.Trim().ToLower();
        return query.Where(a => a.Title.ToLower().Contains(normalizedSearch));
    }

    private static IQueryable<Alert> ApplyTagFilter(IQueryable<Alert> query, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return query;
        }

        var normalizedTag = NormalizeTag(tag);
        return query.Where(a => a.AlertTags.Any(at => at.Tag.NormalizedValue == normalizedTag));
    }

    private static IQueryable<Alert> ApplyPaging(IQueryable<Alert> query, int page, int pageSize)
    {
        return query.Skip((page - 1) * pageSize);
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
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag)
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

    public async Task<Alert?> AddTagsAsync(int alertId, IReadOnlyCollection<string> normalizedTags, CancellationToken cancellationToken = default)
    {
        var alert = await LoadAlertWithTagsAsync(alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var missingNormalizedTags = GetMissingNormalizedTags(alert, normalizedTags);

        if (missingNormalizedTags.Count == 0)
        {
            return alert;
        }

        var tagsByNormalizedValue = await LoadTagsByNormalizedValueAsync(missingNormalizedTags, cancellationToken);
        AddMissingTagsToAlert(alert, missingNormalizedTags, tagsByNormalizedValue);

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    private Task<Alert?> LoadAlertWithTagsAsync(int alertId, CancellationToken cancellationToken)
    {
        return _context.Alerts
            .Include(a => a.AlertTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
    }

    private static List<string> GetMissingNormalizedTags(Alert alert, IReadOnlyCollection<string> normalizedTags)
    {
        var existingNormalizedTags = alert.AlertTags
            .Select(at => at.Tag.NormalizedValue)
            .ToHashSet(StringComparer.Ordinal);

        return normalizedTags
            .Where(t => !existingNormalizedTags.Contains(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<Dictionary<string, Tag>> LoadTagsByNormalizedValueAsync(
        IReadOnlyCollection<string> normalizedTags,
        CancellationToken cancellationToken)
    {
        return await _context.Tags
            .Where(t => normalizedTags.Contains(t.NormalizedValue))
            .ToDictionaryAsync(t => t.NormalizedValue, StringComparer.Ordinal, cancellationToken);
    }

    private void AddMissingTagsToAlert(
        Alert alert,
        IReadOnlyCollection<string> missingNormalizedTags,
        IDictionary<string, Tag> tagsByNormalizedValue)
    {
        foreach (var normalizedTag in missingNormalizedTags)
        {
            if (!tagsByNormalizedValue.TryGetValue(normalizedTag, out var tag))
            {
                tag = new Tag
                {
                    Value = normalizedTag,
                    NormalizedValue = normalizedTag
                };

                _context.Tags.Add(tag);
            }

            alert.AlertTags.Add(new AlertTag
            {
                Alert = alert,
                Tag = tag
            });
        }
    }

    public async Task<bool> RemoveTagAsync(int alertId, string normalizedTag, CancellationToken cancellationToken = default)
    {
        var alertTag = await _context.AlertTags
            .Include(at => at.Tag)
            .SingleOrDefaultAsync(
                at => at.AlertId == alertId && at.Tag.NormalizedValue == normalizedTag,
                cancellationToken);

        if (alertTag is null)
        {
            return false;
        }

        _context.AlertTags.Remove(alertTag);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToLowerInvariant();
    }
}
