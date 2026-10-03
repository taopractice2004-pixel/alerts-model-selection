using AlertService.Common.Enums;
using AlertService.Data.SQL.Repositories;
using AlertService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
    public async Task GetDailyCountsAsync_WhenEmpty_ReturnsEmpty()
    {
        var result = await _repository.GetDailyCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDailyCountsAsync_GroupsByUtcDayAndSeverity_IncludingInactive()
    {
        await _repository.AddAsync(NewAlert("A", Severity.High, new DateTime(2026, 9, 1, 0, 30, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("B", Severity.High, new DateTime(2026, 9, 1, 23, 59, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("C", Severity.Low, new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("D", Severity.High, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailyCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result.Single(r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.High).Count);
        Assert.Equal(1, result.Single(r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.Low).Count);
        Assert.Equal(1, result.Single(r => r.Date == new DateTime(2026, 9, 2) && r.Severity == Severity.High).Count);
    }

    [Fact]
    public async Task GetDailyCountsAsync_IncludesFromUtc_AndExcludesBeforeFromAndAtOrAfterToExclusive()
    {
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var toExclusive = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Before", Severity.Low, from.AddTicks(-1)));
        await _repository.AddAsync(NewAlert("AtFrom", Severity.Medium, from));
        await _repository.AddAsync(NewAlert("LastTick", Severity.High, toExclusive.AddTicks(-1)));
        await _repository.AddAsync(NewAlert("AtToExclusive", Severity.Critical, toExclusive));

        var result = await _repository.GetDailyCountsAsync(from, toExclusive);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.Medium && r.Count == 1);
        Assert.Contains(result, r => r.Date == new DateTime(2026, 9, 2) && r.Severity == Severity.High && r.Count == 1);
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

    private static Alert NewAlertWithTags(string title, DateTime created, params Tag[] tags)
    {
        var alert = NewAlert(title, created: created);
        foreach (var tag in tags)
        {
            alert.Tags.Add(tag);
        }

        return alert;
    }

    [Fact]
    public async Task UpdateAsync_PersistsManyToManyTags_AndGetByIdAsyncReloadsThem()
    {
        var added = await _repository.AddAsync(NewAlert());
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(added.Id);
        loaded!.Tags.Add(new Tag { Name = "urgent" });
        loaded.Tags.Add(new Tag { Name = "network" });
        await _repository.UpdateAsync(loaded);
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(added.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new[] { "network", "urgent" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(2, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WhenAlertHasNoTags_ReturnsEmptyTags()
    {
        var added = await _repository.AddAsync(NewAlert());
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(added.Id);

        Assert.Empty(result!.Tags);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTagsOnItems()
    {
        var urgent = new Tag { Name = "urgent" };
        await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), urgent, new Tag { Name = "network" }));
        await _repository.AddAsync(NewAlert("Untagged", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        _context.ChangeTracker.Clear();

        var result = await _repository.GetAllAsync();

        var tagged = result.Items.Single(a => a.Title == "Tagged");
        var untagged = result.Items.Single(a => a.Title == "Untagged");
        Assert.Equal(new[] { "network", "urgent" }, tagged.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Empty(untagged.Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsHavingThatTag()
    {
        var urgent = new Tag { Name = "urgent" };
        var network = new Tag { Name = "network" };
        await _repository.AddAsync(NewAlertWithTags("Urgent only", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), urgent));
        await _repository.AddAsync(NewAlertWithTags("Network only", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), network));
        await _repository.AddAsync(NewAlertWithTags("Both", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), urgent, network));
        await _repository.AddAsync(NewAlert("No tags", created: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(tag: "urgent");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Both", "Urgent only" }, result.Items.Select(a => a.Title).ToArray());
        Assert.All(result.Items, a => Assert.Contains(a.Tags, t => t.Name == "urgent"));
    }

    [Theory]
    [InlineData("URGENT")]
    [InlineData("  Urgent  ")]
    public async Task GetAllAsync_WithTag_MatchesCaseInsensitivelyAndTrimmed(string tag)
    {
        await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new Tag { Name = "urgent" }));
        await _repository.AddAsync(NewAlert("Untagged", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(tag: tag);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetAllAsync_WithUnknownTag_ReturnsEmptyResult()
    {
        await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new Tag { Name = "urgent" }));

        var result = await _repository.GetAllAsync(tag: "nonexistent");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_WithBlankTag_AppliesNoTagFilter(string? tag)
    {
        await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new Tag { Name = "urgent" }));
        await _repository.AddAsync(NewAlert("Untagged", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetAllAsync(tag: tag);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndOtherFilters_ReturnsOnlyAlertsMatchingAllCriteria()
    {
        var urgent = new Tag { Name = "urgent" };
        var matching = NewAlertWithTags("Disk in range", new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), urgent);
        matching.Severity = Severity.Critical;
        var inactive = NewAlertWithTags("Disk inactive", new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc), urgent);
        inactive.Severity = Severity.Critical;
        inactive.IsActive = false;
        var lowerSeverity = NewAlertWithTags("Disk lower severity", new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), urgent);
        lowerSeverity.Severity = Severity.Low;
        var otherTitle = NewAlertWithTags("CPU spike", new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), urgent);
        otherTitle.Severity = Severity.Critical;
        var outOfRange = NewAlertWithTags("Disk out of range", new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), urgent);
        outOfRange.Severity = Severity.Critical;
        var noTag = NewAlert("Disk no tag", Severity.Critical, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc));
        foreach (var alert in new[] { matching, inactive, lowerSeverity, otherTitle, outOfRange, noTag })
        {
            await _repository.AddAsync(alert);
        }

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            search: "disk",
            tag: "urgent");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk in range", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_CountsAndPagesFilteredAlertsOnly()
    {
        var urgent = new Tag { Name = "urgent" };
        for (var i = 1; i <= 5; i++)
        {
            await _repository.AddAsync(NewAlertWithTags($"Tagged {i}", new DateTime(2026, 1, i, 0, 0, 0, DateTimeKind.Utc), urgent));
        }

        await _repository.AddAsync(NewAlert("Untagged newest", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var pageTwo = await _repository.GetAllAsync(tag: "urgent", page: 2, pageSize: 2);
        var pageThree = await _repository.GetAllAsync(tag: "urgent", page: 3, pageSize: 2);

        Assert.Equal(5, pageTwo.TotalCount);
        Assert.Equal(new[] { "Tagged 3", "Tagged 2" }, pageTwo.Items.Select(a => a.Title).ToArray());
        Assert.Equal(5, pageThree.TotalCount);
        Assert.Equal(new[] { "Tagged 1" }, pageThree.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithTag_TranslatesToSqlUnderRelationalProvider()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(connection).Options;
        await using var context = new AlertDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new AlertRepository(context);

        var urgent = new Tag { Name = "urgent" };
        await repository.AddAsync(NewAlertWithTags("Older tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), urgent));
        await repository.AddAsync(NewAlertWithTags("Newer tagged", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), urgent, new Tag { Name = "network" }));
        await repository.AddAsync(NewAlert("Untagged", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        context.ChangeTracker.Clear();

        var result = await repository.GetAllAsync(tag: "URGENT", pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("Newer tagged", item.Title);
        Assert.Equal(new[] { "network", "urgent" }, item.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetTagsByNamesAsync_ReturnsOnlyExistingMatches()
    {
        await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new Tag { Name = "urgent" }, new Tag { Name = "network" }));

        var result = await _repository.GetTagsByNamesAsync(new[] { "urgent", "missing" });

        Assert.Equal("urgent", Assert.Single(result).Name);
    }

    [Fact]
    public async Task GetTagsByNamesAsync_WithNoMatches_ReturnsEmpty()
    {
        var result = await _repository.GetTagsByNamesAsync(new[] { "missing" });

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_AfterRemovingTagAssignment_KeepsTagRow()
    {
        var added = await _repository.AddAsync(NewAlertWithTags("Tagged", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new Tag { Name = "urgent" }, new Tag { Name = "network" }));
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(added.Id);
        loaded!.Tags.Remove(loaded.Tags.Single(t => t.Name == "urgent"));
        await _repository.UpdateAsync(loaded);
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(added.Id);
        Assert.Equal(new[] { "network" }, reloaded!.Tags.Select(t => t.Name));
        Assert.True(await _context.Tags.AnyAsync(t => t.Name == "urgent"));
    }

    [Fact]
    public async Task DeleteAsync_LeavesOtherAlertsTagsIntact()
    {
        var shared = new Tag { Name = "shared" };
        var toDelete = await _repository.AddAsync(NewAlertWithTags("To delete", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), shared));
        var toKeep = await _repository.AddAsync(NewAlertWithTags("To keep", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), shared));
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(toDelete.Id);
        await _repository.DeleteAsync(loaded!);
        _context.ChangeTracker.Clear();

        var kept = await _repository.GetByIdAsync(toKeep.Id);
        Assert.Equal(new[] { "shared" }, kept!.Tags.Select(t => t.Name));
        Assert.Null(await _repository.GetByIdAsync(toDelete.Id));
    }

    private static readonly DateTime DuplicateCutoff = new(2026, 9, 1, 11, 45, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MatchesTitleCaseInsensitively()
    {
        var added = await _repository.AddAsync(NewAlert("Disk Full", Severity.High, DuplicateCutoff.AddMinutes(5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("DISK full", Severity.High, DuplicateCutoff);

        Assert.NotNull(result);
        Assert.Equal(added.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_DifferentSeverity_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.Low, DuplicateCutoff.AddMinutes(5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_InactiveMatch_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff.AddMinutes(5), isActive: false));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_DifferentTitle_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("CPU spike", Severity.High, DuplicateCutoff.AddMinutes(5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_OlderThanCutoff_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff.AddSeconds(-1)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_ExactlyAtCutoff_ReturnsMatch()
    {
        var added = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Equal(added.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MultipleMatches_ReturnsMostRecent()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff.AddMinutes(1)));
        var newest = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff.AddMinutes(9)));
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateCutoff.AddMinutes(4)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Equal(newest.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_LoadsTags()
    {
        await _repository.AddAsync(NewAlertWithTags("Disk full", DuplicateCutoff.AddMinutes(5), new Tag { Name = "urgent" }, new Tag { Name = "network" }));
        _context.ChangeTracker.Clear();

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateCutoff);

        Assert.Equal(new[] { "network", "urgent" }, result!.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
    }
}
