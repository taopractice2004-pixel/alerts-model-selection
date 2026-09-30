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
            var normalized = tag.Trim().ToLower();
            query = query.Where(a => a.Tags.Any(t => t.NormalizedName == normalized));
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

    public Task<Alert?> FindActiveByTitleAndSeveritySinceAsync(string title, Severity severity, DateTime since, CancellationToken cancellationToken = default)
    {
        if (title is null) throw new ArgumentNullException(nameof(title));

        var normalized = title.Trim().ToLowerInvariant();
        return _context.Alerts
            .AsNoTracking()
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.IsActive
                                      && a.Severity == severity
                                      && a.CreatedDate >= since
                                      && a.Title.ToLower() == normalized, cancellationToken);
    }

    public async Task<Alert?> AddTagsAsync(int alertId, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts.Include(a => a.Tags).SingleOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert is null)
        {
            return null;
        }

        var normalizedInputs = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Determine existing normalized names
        var existingCount = alert.Tags.Count;

        // Find or create Tag entities for the normalized inputs
        foreach (var input in normalizedInputs)
        {
            var normalized = input.ToLowerInvariant();
            if (alert.Tags.Any(t => t.NormalizedName == normalized))
            {
                continue; // already assigned
            }

            // Reuse existing global tag if present
            var existingTag = await _context.Set<Tag>().SingleOrDefaultAsync(t => t.NormalizedName == normalized, cancellationToken);
            if (existingTag is not null)
            {
                alert.Tags.Add(existingTag);
            }
            else
            {
                var newTag = new Tag { Name = input, NormalizedName = normalized };
                alert.Tags.Add(newTag);
            }

            existingCount++;
            if (existingCount > AlertConstants.MaxTagsPerAlert)
            {
                // enforce per-alert limit
                throw new InvalidOperationException($"An alert may have at most {AlertConstants.MaxTagsPerAlert} tags.");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<bool> RemoveTagAsync(int alertId, string tag, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts.Include(a => a.Tags).SingleOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert is null) return false;

        var normalized = tag.Trim().ToLowerInvariant();
        var existing = alert.Tags.SingleOrDefault(t => t.NormalizedName == normalized);
        if (existing is null) return false;

        alert.Tags.Remove(existing);
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

    public async Task<IReadOnlyList<AlertService.Data.Interfaces.DailyAlertTrend>> GetTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));

        var utcToday = DateTime.UtcNow.Date;
        var startDate = utcToday.AddDays(-(days - 1));

        var raw = await _context.Alerts
            .AsNoTracking()
            .Where(a => a.CreatedDate >= startDate && a.CreatedDate <= utcToday.AddDays(1).AddTicks(-1))
            .GroupBy(a => a.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Low = g.Count(a => a.Severity == Severity.Low),
                Medium = g.Count(a => a.Severity == Severity.Medium),
                High = g.Count(a => a.Severity == Severity.High),
                Critical = g.Count(a => a.Severity == Severity.Critical)
            })
            .ToListAsync(cancellationToken);

        var map = raw.ToDictionary(r => r.Date, r => r);
        var result = new List<AlertService.Data.Interfaces.DailyAlertTrend>();
        for (var d = startDate; d <= utcToday; d = d.AddDays(1))
        {
            if (map.TryGetValue(d, out var r))
            {
                result.Add(new AlertService.Data.Interfaces.DailyAlertTrend { Date = r.Date, Low = r.Low, Medium = r.Medium, High = r.High, Critical = r.Critical });
            }
            else
            {
                result.Add(new AlertService.Data.Interfaces.DailyAlertTrend { Date = d, Low = 0, Medium = 0, High = 0, Critical = 0 });
            }
        }

        return result;
    }
}
