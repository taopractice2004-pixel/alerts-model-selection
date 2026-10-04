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

    public Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetAllAsync(
        bool? isActive = null,
        Severity? severity = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        string? search = null,
        string sortBy = AlertConstants.SortByCreatedDate,
        string sortDirection = AlertConstants.SortDirectionDesc,
        int page = AlertConstants.DefaultPageNumber,
        int pageSize = AlertConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return GetAllAsync(
            isActive,
            severity,
            createdFrom,
            createdTo,
            search,
            tag: null,
            sortBy,
            sortDirection,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetAllAsync(
        bool? isActive,
        Severity? severity,
        DateTime? createdFrom,
        DateTime? createdTo,
        string? search,
        string? tag,
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
            var normalizedTag = NormalizeTag(tag);
            query = query.Where(alert => alert.AlertTags.Any(alertTag => alertTag.Tag.NormalizedName == normalizedTag));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, sortBy, sortDirection);

        var items = await query
            .Include(alert => alert.AlertTags)
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

    public async Task<IReadOnlyList<(DateTime DateUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>> GetDailyTrendsAsync(
        DateTime createdFromUtcInclusive,
        DateTime createdToUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var trends = await _context.Alerts
            .AsNoTracking()
            .Where(alert => alert.CreatedDate >= createdFromUtcInclusive && alert.CreatedDate < createdToUtcExclusive)
            .GroupBy(alert => alert.CreatedDate.Date)
            .Select(group => new
            {
                DateUtc = group.Key,
                TotalCount = group.Count(),
                LowCount = group.Count(alert => alert.Severity == Severity.Low),
                MediumCount = group.Count(alert => alert.Severity == Severity.Medium),
                HighCount = group.Count(alert => alert.Severity == Severity.High),
                CriticalCount = group.Count(alert => alert.Severity == Severity.Critical)
            })
            .OrderBy(trend => trend.DateUtc)
            .ToListAsync(cancellationToken);

        return trends
            .Select(trend => (
                DateTime.SpecifyKind(trend.DateUtc, DateTimeKind.Utc),
                trend.TotalCount,
                trend.LowCount,
                trend.MediumCount,
                trend.HighCount,
                trend.CriticalCount))
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

    public Task<Alert?> FindActiveDuplicateAsync(
        string title,
        Severity severity,
        DateTime createdFromUtc,
        DateTime createdToUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return _context.Alerts
            .AsNoTracking()
            .Include(alert => alert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .Where(alert => alert.IsActive)
            .Where(alert => alert.Severity == severity)
            .Where(alert => alert.CreatedDate >= createdFromUtc && alert.CreatedDate <= createdToUtc)
            .Where(alert => alert.Title.ToLower() == normalizedTitle)
            .OrderByDescending(alert => alert.CreatedDate)
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
        if (_context.Entry(alert).State == EntityState.Detached)
        {
            _context.Alerts.Update(alert);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AlertTagMutationResult> AddTagsAsync(int alertId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(existingAlert => existingAlert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return new AlertTagMutationResult
            {
                Status = AlertTagMutationStatus.AlertNotFound
            };
        }

        var requestedTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var trimmedTag = tag.Trim();
            var normalizedTag = NormalizeTag(trimmedTag);
            if (!requestedTags.ContainsKey(normalizedTag))
            {
                requestedTags[normalizedTag] = trimmedTag;
            }
        }

        var existingNormalizedTags = alert.AlertTags
            .Select(alertTag => alertTag.Tag.NormalizedName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tagsToAdd = requestedTags
            .Where(requestedTag => !existingNormalizedTags.Contains(requestedTag.Key))
            .ToList();

        if (alert.AlertTags.Count + tagsToAdd.Count > AlertConstants.MaxTagsPerAlert)
        {
            return new AlertTagMutationResult
            {
                Status = AlertTagMutationStatus.ValidationFailed,
                Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["Tags"] = [$"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags."]
                }
            };
        }

        if (tagsToAdd.Count == 0)
        {
            return new AlertTagMutationResult
            {
                Status = AlertTagMutationStatus.Success,
                Alert = alert
            };
        }

        var normalizedNames = tagsToAdd.Select(tagToAdd => tagToAdd.Key).ToList();
        var existingTags = await _context.Tags
            .Where(existingTag => normalizedNames.Contains(existingTag.NormalizedName))
            .ToListAsync(cancellationToken);

        var existingTagsByNormalizedName = existingTags.ToDictionary(existingTag => existingTag.NormalizedName, StringComparer.OrdinalIgnoreCase);

        foreach (var tagToAdd in tagsToAdd)
        {
            if (!existingTagsByNormalizedName.TryGetValue(tagToAdd.Key, out var tagEntity))
            {
                tagEntity = new Tag
                {
                    Name = tagToAdd.Value,
                    NormalizedName = tagToAdd.Key
                };

                _context.Tags.Add(tagEntity);
                existingTagsByNormalizedName[tagToAdd.Key] = tagEntity;
            }

            alert.AlertTags.Add(new AlertTag
            {
                Alert = alert,
                Tag = tagEntity
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new AlertTagMutationResult
        {
            Status = AlertTagMutationStatus.Success,
            Alert = alert
        };
    }

    public async Task<AlertTagMutationResult> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeTag(tag);
        var alert = await _context.Alerts
            .Include(existingAlert => existingAlert.AlertTags)
            .ThenInclude(alertTag => alertTag.Tag)
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return new AlertTagMutationResult
            {
                Status = AlertTagMutationStatus.AlertNotFound
            };
        }

        var alertTag = alert.AlertTags
            .FirstOrDefault(existingAlertTag => string.Equals(existingAlertTag.Tag.NormalizedName, normalizedTag, StringComparison.OrdinalIgnoreCase));

        if (alertTag is null)
        {
            return new AlertTagMutationResult
            {
                Status = AlertTagMutationStatus.TagAssignmentNotFound
            };
        }

        _context.AlertTags.Remove(alertTag);
        await _context.SaveChangesAsync(cancellationToken);

        return new AlertTagMutationResult
        {
            Status = AlertTagMutationStatus.Success,
            Alert = alert
        };
    }

    private static string NormalizeTag(string tag)
    {
        return tag.Trim().ToUpperInvariant();
    }
}
