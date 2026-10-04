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
    public async Task GetDailySeverityCountsAsync_WhenEmpty_ReturnsEmpty()
    {
        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_GroupsByUtcDayAndSeverity()
    {
        await _repository.AddAsync(NewAlert("a", Severity.High, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("b", Severity.High, new DateTime(2026, 9, 1, 23, 59, 59, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("c", Severity.Low, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("d", Severity.High, new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result.Single(r => r.Date.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.High).Count);
        Assert.Equal(1, result.Single(r => r.Date.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.Low).Count);
        Assert.Equal(1, result.Single(r => r.Date.Date == new DateTime(2026, 9, 2) && r.Severity == Severity.High).Count);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_ExcludesAlertsOutsideWindow_AndIncludesStartBoundary()
    {
        await _repository.AddAsync(NewAlert("before", Severity.Low, new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("at start", Severity.Medium, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("at end", Severity.Critical, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("future", Severity.High, new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        var row = Assert.Single(result);
        Assert.Equal(Severity.Medium, row.Severity);
        Assert.Equal(1, row.Count);
    }

    [Fact]
    public async Task GetDailySeverityCountsAsync_TranslatesGroupByUnderRelationalQuery()
    {
        await using var db = await SqliteDb.CreateAsync();
        await db.Repository.AddAsync(NewAlert("a", Severity.High, new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc)));
        await db.Repository.AddAsync(NewAlert("b", Severity.High, new DateTime(2026, 9, 1, 22, 0, 0, DateTimeKind.Utc)));
        await db.Repository.AddAsync(NewAlert("c", Severity.Low, new DateTime(2026, 9, 2, 5, 0, 0, DateTimeKind.Utc)));
        await db.Repository.AddAsync(NewAlert("out", Severity.Low, new DateTime(2026, 8, 30, 5, 0, 0, DateTimeKind.Utc)));

        var result = await db.Repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Single(r => r.Date.Date == new DateTime(2026, 9, 1) && r.Severity == Severity.High).Count);
        Assert.Equal(1, result.Single(r => r.Date.Date == new DateTime(2026, 9, 2) && r.Severity == Severity.Low).Count);
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

    // Tag tests use SQLite so the join table, unique index and cascade behave like a relational database.
    private sealed class SqliteDb : IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public AlertDbContext Context { get; private set; } = null!;
        public AlertRepository Repository { get; private set; } = null!;

        public static async Task<SqliteDb> CreateAsync()
        {
            var db = new SqliteDb();
            await db._connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(db._connection).Options;
            db.Context = new AlertDbContext(options);
            await db.Context.Database.EnsureCreatedAsync();
            db.Repository = new AlertRepository(db.Context);
            return db;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private static async Task<int> CountJoinRowsAsync(AlertDbContext context) =>
        await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"AlertTags\"").SingleAsync();

    [Fact]
    public async Task AddTagsAsync_CreatesTagRows_AndLinksThemToAlert()
    {
        await using var db = await SqliteDb.CreateAsync();
        var alert = await db.Repository.AddAsync(NewAlert());

        await db.Repository.AddTagsAsync(alert, new[] { "prod", "db" });

        db.Context.ChangeTracker.Clear();
        var reloaded = await db.Repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "db", "prod" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Equal(2, await db.Context.Tags.CountAsync());
        Assert.Equal(2, await CountJoinRowsAsync(db.Context));
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRow_CaseInsensitively_AcrossAlerts()
    {
        await using var db = await SqliteDb.CreateAsync();
        var first = await db.Repository.AddAsync(NewAlert("First"));
        var second = await db.Repository.AddAsync(NewAlert("Second"));
        await db.Repository.AddTagsAsync(first, new[] { "Prod" });

        await db.Repository.AddTagsAsync(second, new[] { "prod" });

        db.Context.ChangeTracker.Clear();
        var tag = await db.Context.Tags.SingleAsync();
        Assert.Equal("Prod", tag.Name);
        Assert.Equal(2, await CountJoinRowsAsync(db.Context));
    }

    [Fact]
    public async Task Tags_UniqueIndexOnName_RejectsDuplicateNames()
    {
        await using var db = await SqliteDb.CreateAsync();
        db.Context.Tags.Add(new Tag { Name = "prod" });
        await db.Context.SaveChangesAsync();
        db.Context.Tags.Add(new Tag { Name = "prod" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesAssignment_ButKeepsTagRowForOtherAlerts()
    {
        await using var db = await SqliteDb.CreateAsync();
        var first = await db.Repository.AddAsync(NewAlert("First"));
        var second = await db.Repository.AddAsync(NewAlert("Second"));
        await db.Repository.AddTagsAsync(first, new[] { "prod", "db" });
        await db.Repository.AddTagsAsync(second, new[] { "prod" });

        await db.Repository.RemoveTagAsync(first, first.Tags.First(t => t.Name == "prod"));

        db.Context.ChangeTracker.Clear();
        var reloadedFirst = await db.Repository.GetByIdAsync(first.Id);
        var reloadedSecond = await db.Repository.GetByIdAsync(second.Id);
        Assert.Equal(new[] { "db" }, reloadedFirst!.Tags.Select(t => t.Name).ToArray());
        Assert.Equal(new[] { "prod" }, reloadedSecond!.Tags.Select(t => t.Name).ToArray());
        Assert.Equal(2, await db.Context.Tags.CountAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenLastAlertLosesTag_KeepsOrphanedTagRow()
    {
        await using var db = await SqliteDb.CreateAsync();
        var alert = await db.Repository.AddAsync(NewAlert());
        await db.Repository.AddTagsAsync(alert, new[] { "prod" });

        await db.Repository.RemoveTagAsync(alert, alert.Tags.Single());

        Assert.Equal(1, await db.Context.Tags.CountAsync());
        Assert.Equal(0, await CountJoinRowsAsync(db.Context));
    }

    [Fact]
    public async Task DeleteAsync_CascadesJoinRows_ButKeepsTagRows()
    {
        await using var db = await SqliteDb.CreateAsync();
        var alert = await db.Repository.AddAsync(NewAlert());
        await db.Repository.AddTagsAsync(alert, new[] { "prod", "db" });

        await db.Repository.DeleteAsync(alert);

        Assert.Equal(0, await CountJoinRowsAsync(db.Context));
        Assert.Equal(2, await db.Context.Tags.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTags()
    {
        await using var db = await SqliteDb.CreateAsync();
        var alert = await db.Repository.AddAsync(NewAlert());
        await db.Repository.AddTagsAsync(alert, new[] { "prod" });
        db.Context.ChangeTracker.Clear();

        var result = await db.Repository.GetByIdAsync(alert.Id);

        Assert.Equal(new[] { "prod" }, result!.Tags.Select(t => t.Name).ToArray());
    }

    private static async Task<Alert> AddTaggedAsync(SqliteDb db, string title, Severity severity, DateTime created, bool isActive, params string[] tags)
    {
        var alert = await db.Repository.AddAsync(NewAlert(title, severity, created, isActive));
        if (tags.Length > 0)
        {
            await db.Repository.AddTagsAsync(alert, tags);
        }

        return alert;
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("PROD")]
    [InlineData("  Prod  ")]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsWithThatTag_CaseInsensitively(string filter)
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Tagged", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "Prod", "db");
        await AddTaggedAsync(db, "Other tag", Severity.High, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), true, "staging");
        await AddTaggedAsync(db, "No tags", Severity.High, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), true);

        var result = await db.Repository.GetAllAsync(tag: filter);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", Assert.Single(result.Items).Title);
        Assert.Equal(new[] { "db", "Prod" }, result.Items[0].Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithUnknownTag_ReturnsEmptyResult()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Tagged", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod");

        var result = await db.Repository.GetAllAsync(tag: "nope");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithBlankTag_DoesNotFilter()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Tagged", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "Untagged", Severity.High, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), true);

        var result = await db.Repository.GetAllAsync(tag: "   ");

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithTagMatchesWholeNameOnly_NotSubstring()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Production", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "production");

        var result = await db.Repository.GetAllAsync(tag: "prod");

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndOtherFilters_AppliesAllFiltersTogether()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Disk match", Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "Disk wrong tag", Severity.Critical, new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc), true, "staging");
        await AddTaggedAsync(db, "Disk inactive", Severity.Critical, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), false, "prod");
        await AddTaggedAsync(db, "Disk low severity", Severity.Low, new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "Disk out of range", Severity.Critical, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "CPU other title", Severity.Critical, new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), true, "prod");

        var result = await db.Repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
            search: "disk",
            tag: "PROD");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk match", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_TotalCountAndPagingReflectFilteredSet()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "A", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "B", Severity.High, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod", "db");
        await AddTaggedAsync(db, "C", Severity.High, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod");
        await AddTaggedAsync(db, "D", Severity.High, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), true, "staging");

        var page1 = await db.Repository.GetAllAsync(tag: "prod", page: 1, pageSize: 2);
        var page2 = await db.Repository.GetAllAsync(tag: "prod", page: 2, pageSize: 2);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(new[] { "C", "B" }, page1.Items.Select(a => a.Title).ToArray());
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(new[] { "A" }, page2.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithoutTagFilter_StillLoadsTagsForEachAlert()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "A", Severity.High, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, "prod", "db");

        var result = await db.Repository.GetAllAsync();

        Assert.Equal(2, Assert.Single(result.Items).Tags.Count);
    }

    private static readonly DateTime DuplicateNow = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MatchesTitleCaseInsensitively()
    {
        await _repository.AddAsync(NewAlert("Disk Full", Severity.High, DuplicateNow.AddMinutes(-5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("disk FULL", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.NotNull(result);
        Assert.Equal("Disk Full", result!.Title);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithDifferentSeverity_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.Low, DuplicateNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithInactiveMatch_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-5), isActive: false));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithMatchOlderThanWindow_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-15).AddSeconds(-1)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithMatchExactlyAtWindowStart_ReturnsAlert()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-15)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithMultipleMatches_ReturnsMostRecent()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-10)));
        var newest = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-2)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.Equal(newest.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_WithDifferentTitle_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DuplicateNow.AddMinutes(-5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("CPU high", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_IncludesTags()
    {
        await using var db = await SqliteDb.CreateAsync();
        await AddTaggedAsync(db, "Disk full", Severity.High, DuplicateNow.AddMinutes(-5), true, "prod", "db");

        var result = await db.Repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DuplicateNow.AddMinutes(-15));

        Assert.Equal(2, result!.Tags.Count);
    }
}
