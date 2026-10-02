using AlertService.Common.Enums;
using AlertService.Data.SQL.Repositories;
using AlertService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AlertService.Data.SQL.Tests.Repositories;

public class AlertRepositoryTests : IDisposable
{
    private readonly AlertDbContext _context;
    private readonly AlertRepository _repository;

    public AlertRepositoryTests()
    {
        // Each test class instance gets its own isolated in-memory database.
        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseInMemoryDatabase($"AlertDb_{Guid.NewGuid()}")
            .Options;

        _context = new AlertDbContext(options);
        _repository = new AlertRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static Alert NewAlert(string title = "Disk full", Severity severity = Severity.High, DateTime? created = null, bool isActive = true) => new()
    {
        Title = title,
        Description = "Test description",
        Severity = severity,
        CreatedDate = created ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        IsActive = isActive
    };

    [Fact]
    public async Task AddAsync_PersistsAlert_AndAssignsId()
    {
        var result = await _repository.AddAsync(NewAlert());

        Assert.True(result.Id > 0);
        Assert.Equal(1, await _context.Alerts.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAlerts_NewestFirst()
    {
        await _repository.AddAsync(NewAlert("Older", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Newer", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Newer", result.Items[0].Title);
        Assert.Equal("Older", result.Items[1].Title);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync();

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveTrue_ReturnsOnlyActiveAlerts()
    {
        await _repository.AddAsync(NewAlert("Active older", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Inactive newest", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Active newest", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetAllAsync(true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, a => Assert.True(a.IsActive));
        Assert.Equal("Active newest", result.Items[0].Title);
        Assert.Equal("Active older", result.Items[1].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFalse_ReturnsOnlyInactiveAlerts()
    {
        await _repository.AddAsync(NewAlert("Active", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Inactive older", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Inactive newest", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));

        var result = await _repository.GetAllAsync(false);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, a => Assert.False(a.IsActive));
        Assert.Equal("Inactive newest", result.Items[0].Title);
        Assert.Equal("Inactive older", result.Items[1].Title);
    }

    [Theory]
    [InlineData(Severity.Low, "Low alert")]
    [InlineData(Severity.Medium, "Medium alert")]
    [InlineData(Severity.High, "High alert")]
    [InlineData(Severity.Critical, "Critical alert")]
    public async Task GetAllAsync_WithSeverity_ReturnsOnlyMatchingSeverity(Severity severity, string expectedTitle)
    {
        await _repository.AddAsync(NewAlert("Low alert", Severity.Low, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Medium alert", Severity.Medium, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("High alert", Severity.High, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Critical alert", Severity.Critical, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(severity: severity);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(severity, result.Items[0].Severity);
        Assert.Equal(expectedTitle, result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveAndSeverity_ReturnsOnlyMatchingAlerts()
    {
        await _repository.AddAsync(NewAlert("Critical active", Severity.Critical, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Critical inactive", Severity.Critical, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("High active", Severity.High, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetAllAsync(isActive: true, severity: Severity.Critical);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.True(result.Items[0].IsActive);
        Assert.Equal(Severity.Critical, result.Items[0].Severity);
        Assert.Equal("Critical active", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_ReturnsCaseInsensitiveTitleMatches()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("CPU spike", Severity.Critical, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("DISK controller warning", Severity.Medium, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(search: "disk");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("DISK controller warning", result.Items[0].Title);
        Assert.Equal("Disk full", result.Items[1].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithCreatedFrom_ReturnsAlertsOnOrAfterBoundary()
    {
        await _repository.AddAsync(NewAlert("Before range", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("At boundary", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("After boundary", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "After boundary", "At boundary" }, result.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithCreatedTo_ReturnsAlertsOnOrBeforeBoundary()
    {
        await _repository.AddAsync(NewAlert("Before boundary", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("At boundary", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("After boundary", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(createdTo: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "At boundary", "Before boundary" }, result.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithCreatedRange_AppliesBothInclusiveBounds()
    {
        await _repository.AddAsync(NewAlert("Before range", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Range start", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("In range", created: new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Range end", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("After range", created: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "Range end", "In range", "Range start" }, result.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithCreatedRangeAndExistingFilters_ReturnsOnlyAlertsMatchingAllCriteria()
    {
        await _repository.AddAsync(NewAlert("Disk before range", Severity.Critical, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("CPU in range", Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Disk inactive in range", Severity.Critical, new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Disk lower severity", Severity.High, new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Disk in range", Severity.Critical, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Disk after range", Severity.Critical, new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            search: "disk");

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk in range", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithCreatedRangeThatMatchesNothing_ReturnsEmptyResult()
    {
        await _repository.AddAsync(NewAlert("Older alert", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Newer alert", created: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithSortByTitleAscending_ReturnsAlphabeticalPage()
    {
        await _repository.AddAsync(NewAlert("Zulu", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Alpha", Severity.Low, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Mike", Severity.Medium, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(sortBy: "title", sortDirection: "asc", page: 1, pageSize: 2);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "Alpha", "Mike" }, result.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithSortBySeverityDescending_UsesBusinessSeverityRankUnderRelationalQuery()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AlertDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new AlertRepository(context);

        await repository.AddAsync(NewAlert("Medium", Severity.Medium, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Critical", Severity.Critical, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Low", Severity.Low, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("High", Severity.High, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await repository.GetAllAsync(sortBy: "severity", sortDirection: "desc");

        Assert.Equal(new[] { Severity.Critical, Severity.High, Severity.Medium, Severity.Low }, result.Items.Select(a => a.Severity).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WhenPageIsPastLastPage_ReturnsEmptyItemsWithTotalCount()
    {
        await _repository.AddAsync(NewAlert("First", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Second", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(page: 3, pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenEmpty_ReturnsZeroCounts()
    {
        var result = await _repository.GetSummaryAsync();

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.ActiveCount);
        Assert.Equal(0, result.InactiveCount);
        Assert.Equal(0, result.LowCount);
        Assert.Equal(0, result.MediumCount);
        Assert.Equal(0, result.HighCount);
        Assert.Equal(0, result.CriticalCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithMixedAlerts_ReturnsAggregateCounts()
    {
        await _repository.AddAsync(NewAlert("Low active", Severity.Low, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Medium inactive", Severity.Medium, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Critical active", Severity.Critical, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Critical inactive", Severity.Critical, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("High active", Severity.High, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetSummaryAsync();

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.ActiveCount);
        Assert.Equal(2, result.InactiveCount);
        Assert.Equal(1, result.LowCount);
        Assert.Equal(1, result.MediumCount);
        Assert.Equal(1, result.HighCount);
        Assert.Equal(2, result.CriticalCount);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_GroupsByDateAndSeverity()
    {
        await _repository.AddAsync(NewAlert("A", Severity.High, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("B", Severity.High, new DateTime(2026, 9, 1, 15, 30, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("C", Severity.Low, new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("D", Severity.Critical, new DateTime(2026, 8, 31, 9, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.High && r.Count == 2);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.Low && r.Count == 1);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 8, 31) && r.Severity == Severity.Critical && r.Count == 1);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_FromIsInclusive_ToIsExclusive()
    {
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Before from", Severity.Low, from.AddTicks(-1)));
        await _repository.AddAsync(NewAlert("At from", Severity.Medium, from));
        await _repository.AddAsync(NewAlert("Just before to", Severity.High, to.AddTicks(-1)));
        await _repository.AddAsync(NewAlert("At to", Severity.Critical, to));

        var result = await _repository.GetDailySeverityCountsAsync(from, to);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.Medium && r.Count == 1);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.High && r.Count == 1);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_CountsActiveAndInactiveAlerts()
    {
        var created = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Active", Severity.High, created, isActive: true));
        await _repository.AddAsync(NewAlert("Inactive", Severity.High, created, isActive: false));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        var group = Assert.Single(result);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsAlert()
    {
        var added = await _repository.AddAsync(NewAlert("CPU spike", Severity.Critical));

        var result = await _repository.GetByIdAsync(added.Id);

        Assert.NotNull(result);
        Assert.Equal("CPU spike", result!.Title);
        Assert.Equal(Severity.Critical, result.Severity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    private static readonly DateTime DuplicateWindowStart = new(2026, 9, 1, 11, 45, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MatchesTitleCaseInsensitively()
    {
        var added = await _repository.AddAsync(NewAlert("Disk Full", Severity.High, DuplicateWindowStart.AddMinutes(5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("disk FULL", Severity.High, DuplicateWindowStart);

        Assert.NotNull(result);
        Assert.Equal(added.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_DifferentSeverity_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddMinutes(5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.Critical, DuplicateWindowStart);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_InactiveMatch_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddMinutes(5), isActive: false));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateWindowStart);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_OlderThanWindow_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddTicks(-1)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateWindowStart);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_ExactlyAtWindowStart_ReturnsMatch()
    {
        var added = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateWindowStart);

        Assert.Equal(added.Id, result?.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MultipleMatches_ReturnsMostRecent()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddMinutes(2)));
        var newest = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddMinutes(9)));
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateWindowStart.AddMinutes(4)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateWindowStart);

        Assert.Equal(newest.Id, result?.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_ReturnsTags()
    {
        var added = await AddTaggedAlertAsync("Disk full", new[] { "disk", "prod" }, created: DuplicateWindowStart.AddMinutes(5));
        _context.ChangeTracker.Clear();

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateWindowStart);

        Assert.Equal(added.Id, result?.Id);
        Assert.Equal(new[] { "disk", "prod" }, result!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var added = await _repository.AddAsync(NewAlert());

        added.Title = "Updated title";
        added.IsActive = false;
        await _repository.UpdateAsync(added);

        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alerts.SingleAsync(a => a.Id == added.Id);
        Assert.Equal("Updated title", reloaded.Title);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAlert()
    {
        var added = await _repository.AddAsync(NewAlert());

        await _repository.DeleteAsync(added);

        Assert.False(await _context.Alerts.AnyAsync());
    }

    private async Task<Alert> AddTaggedAlertAsync(
        string title,
        string[] tags,
        Severity severity = Severity.High,
        DateTime? created = null,
        bool isActive = true)
    {
        var alert = await _repository.AddAsync(NewAlert(title, severity, created, isActive));
        if (tags.Length > 0)
        {
            await _repository.AddTagsAsync(alert, tags);
        }

        return alert;
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsHavingTag()
    {
        await AddTaggedAlertAsync("Tagged disk", new[] { "disk", "prod" }, created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("Tagged cpu", new[] { "cpu" }, created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("Untagged", Array.Empty<string>(), created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.GetAllAsync(tag: "disk");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged disk", Assert.Single(result.Items).Title);
    }

    [Theory]
    [InlineData("DISK")]
    [InlineData("  Disk  ")]
    public async Task GetAllAsync_WithTag_IsCaseInsensitiveAndTrimmed(string tag)
    {
        await AddTaggedAlertAsync("Tagged disk", new[] { "disk" });
        await AddTaggedAlertAsync("Tagged cpu", new[] { "cpu" });

        var result = await _repository.GetAllAsync(tag: tag);

        Assert.Equal("Tagged disk", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagThatMatchesNothing_ReturnsEmptyResult()
    {
        await AddTaggedAlertAsync("Tagged disk", new[] { "disk" });

        var result = await _repository.GetAllAsync(tag: "network");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_WithNullOrWhitespaceTag_IgnoresFilter(string? tag)
    {
        await AddTaggedAlertAsync("Tagged", new[] { "disk" });
        await AddTaggedAlertAsync("Untagged", Array.Empty<string>());

        var result = await _repository.GetAllAsync(tag: tag);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndOtherFilters_ReturnsOnlyAlertsMatchingAllCriteria()
    {
        var tags = new[] { "disk" };
        await AddTaggedAlertAsync("Disk match", tags, Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        await AddTaggedAlertAsync("Disk inactive", tags, Severity.Critical, new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc), isActive: false);
        await AddTaggedAlertAsync("Disk low severity", tags, Severity.Low, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        await AddTaggedAlertAsync("Disk out of range", tags, Severity.Critical, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        await AddTaggedAlertAsync("Cpu other title", tags, Severity.Critical, new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        await AddTaggedAlertAsync("Disk without tag", Array.Empty<string>(), Severity.Critical, new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), isActive: true);

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            search: "disk",
            tag: "DISK");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk match", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_PagingAndTotalsReflectFilteredSet()
    {
        await AddTaggedAlertAsync("A", new[] { "disk" }, created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("B", new[] { "disk" }, created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("C", new[] { "disk" }, created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("D", new[] { "cpu" }, created: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.GetAllAsync(tag: "disk", page: 2, pageSize: 2);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal("A", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTagsForEachAlert()
    {
        await AddTaggedAlertAsync("Two tags", new[] { "disk", "prod" }, created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddTaggedAlertAsync("No tags", Array.Empty<string>(), created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.GetAllAsync();

        Assert.Equal(new[] { "disk", "prod" }, result.Items[0].Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Empty(result.Items[1].Tags);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTags()
    {
        var added = await AddTaggedAlertAsync("Tagged", new[] { "disk", "prod" });
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(added.Id);

        Assert.NotNull(result);
        Assert.Equal(new[] { "disk", "prod" }, result!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_CreatesNewTagRows_AndAssignsThem()
    {
        var alert = await _repository.AddAsync(NewAlert());

        await _repository.AddTagsAsync(alert, new[] { "disk", "prod" });

        _context.ChangeTracker.Clear();
        Assert.Equal(2, await _context.Tags.CountAsync());
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "disk", "prod" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRow_AcrossAlerts()
    {
        var first = await AddTaggedAlertAsync("First", new[] { "disk" });
        var second = await _repository.AddAsync(NewAlert("Second"));

        await _repository.AddTagsAsync(second, new[] { "disk" });

        _context.ChangeTracker.Clear();
        Assert.Equal(1, await _context.Tags.CountAsync());
        Assert.Equal(new[] { first.Id, second.Id }, (await _repository.GetAllAsync(tag: "disk")).Items.Select(a => a.Id).OrderBy(i => i).ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagAlreadyAssigned_DoesNotDuplicateAssignment()
    {
        var alert = await AddTaggedAlertAsync("Tagged", new[] { "disk" });

        await _repository.AddTagsAsync(alert, new[] { "disk", "prod" });

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "disk", "prod" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Equal(2, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_ReturnsTrue_RemovesAssignment_AndKeepsTagRow()
    {
        var alert = await AddTaggedAlertAsync("Tagged", new[] { "disk", "prod" });

        var removed = await _repository.RemoveTagAsync(alert, "disk");

        Assert.True(removed);
        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "prod" }, reloaded!.Tags.Select(t => t.Name).ToArray());
        Assert.True(await _context.Tags.AnyAsync(t => t.Name == "disk"));
    }

    [Fact]
    public async Task RemoveTagAsync_WhenNotAssigned_ReturnsFalse()
    {
        var alert = await AddTaggedAlertAsync("Tagged", new[] { "prod" });

        var removed = await _repository.RemoveTagAsync(alert, "disk");

        Assert.False(removed);
        Assert.Single(alert.Tags);
    }

    private static async Task<(SqliteConnection Connection, AlertDbContext Context)> CreateSqliteContextAsync(List<string>? commandLog = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var builder = new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(connection);
        if (commandLog is not null)
        {
            builder.LogTo(commandLog.Add, new[] { RelationalEventId.CommandExecuted });
        }

        var context = new AlertDbContext(builder.Options);
        await context.Database.EnsureCreatedAsync();
        return (connection, context);
    }

    [Fact]
    public async Task Schema_CreatesTagsAndAlertTagsTables()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _ = connection;
        await using var __ = context;

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();

        Assert.Contains("Tags", tables);
        Assert.Contains("AlertTags", tables);
    }

    [Fact]
    public async Task DeletingAlert_RemovesItsJoinRows_AndKeepsTagRows()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _ = connection;
        await using var __ = context;
        var repository = new AlertRepository(context);
        var alert = await repository.AddAsync(NewAlert());
        await repository.AddTagsAsync(alert, new[] { "disk", "prod" });
        context.ChangeTracker.Clear();

        // Loaded without Tags so only the database cascade can remove the join rows.
        var untracked = await context.Alerts.SingleAsync(a => a.Id == alert.Id);
        await repository.DeleteAsync(untracked);

        var joinRows = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AlertTags").SingleAsync();
        Assert.Equal(0, joinRows);
        Assert.Equal(2, await context.Tags.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_WithTagsLoaded_KeepsTags_AndDoesNotUpdateTagRows()
    {
        var commands = new List<string>();
        var (connection, context) = await CreateSqliteContextAsync(commands);
        await using var _ = connection;
        await using var __ = context;
        var repository = new AlertRepository(context);
        var added = await repository.AddAsync(NewAlert());
        await repository.AddTagsAsync(added, new[] { "disk" });
        context.ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(added.Id);
        commands.Clear();

        loaded!.Title = "Updated";
        await repository.UpdateAsync(loaded);

        Assert.DoesNotContain(commands, c => c.Contains("UPDATE \"Tags\"", StringComparison.OrdinalIgnoreCase));
        context.ChangeTracker.Clear();
        var reloaded = await repository.GetByIdAsync(added.Id);
        Assert.Equal("Updated", reloaded!.Title);
        Assert.Equal(new[] { "disk" }, reloaded.Tags.Select(t => t.Name).ToArray());
    }
}
