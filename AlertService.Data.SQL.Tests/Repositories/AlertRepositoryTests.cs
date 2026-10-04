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

    // AC2 + AC3: counts are grouped by UTC calendar day and severity.
    [Fact]
    public async Task GetDailySeverityCountsAsync_GroupsByUtcDayAndSeverity()
    {
        await _repository.AddAsync(NewAlert("A", Severity.Low, new DateTime(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("B", Severity.Low, new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("C", Severity.High, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("D", Severity.Low, new DateTime(2026, 9, 2, 1, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result.Single(r => r.DayUtc == new DateTime(2026, 9, 1) && r.Severity == Severity.Low).Count);
        Assert.Equal(1, result.Single(r => r.DayUtc == new DateTime(2026, 9, 1) && r.Severity == Severity.High).Count);
        Assert.Equal(1, result.Single(r => r.DayUtc == new DateTime(2026, 9, 2) && r.Severity == Severity.Low).Count);
    }

    // AC2: the range is half-open [fromUtcInclusive, toUtcExclusive).
    [Fact]
    public async Task GetDailySeverityCountsAsync_RespectsHalfOpenRange()
    {
        await _repository.AddAsync(NewAlert("before", Severity.Low, new DateTime(2026, 8, 31, 23, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("start", Severity.Low, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("inside", Severity.High, new DateTime(2026, 9, 1, 23, 59, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("end", Severity.Low, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(new DateTime(2026, 9, 1), r.DayUtc));
        Assert.Equal(1, result.Single(r => r.Severity == Severity.Low).Count);
        Assert.Equal(1, result.Single(r => r.Severity == Severity.High).Count);
    }

    // AC4: no alerts in the range yields an empty result (the service zero-fills the buckets).
    [Fact]
    public async Task GetDailySeverityCountsAsync_WhenNoAlertsInRange_ReturnsEmpty()
    {
        await _repository.AddAsync(NewAlert("out of range", Severity.Low, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailySeverityCountsAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc));

        Assert.Empty(result);
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

    [Fact]
    public async Task AddTagsToAlertAsync_CreatesTagRows_AndPersistsJoin()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var alert = await repository.AddAsync(NewAlert("Tagged"));

        await repository.AddTagsToAlertAsync(alert, new[] { "database", "prod" });

        context.ChangeTracker.Clear();
        var reloaded = await context.Alerts.Include(a => a.Tags).SingleAsync(a => a.Id == alert.Id);
        Assert.Equal(new[] { "database", "prod" }, reloaded.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Equal(2, await context.Tags.CountAsync());
    }

    [Fact]
    public async Task AddTagsToAlertAsync_ReusesExistingTagRow_CaseInsensitive()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var first = await repository.AddAsync(NewAlert("First"));
        await repository.AddTagsToAlertAsync(first, new[] { "database" });

        var second = await repository.AddAsync(NewAlert("Second"));
        await repository.AddTagsToAlertAsync(second, new[] { "DATABASE" });

        Assert.Equal(1, await context.Tags.CountAsync());
        context.ChangeTracker.Clear();
        var reloaded = await context.Alerts.Include(a => a.Tags).SingleAsync(a => a.Id == second.Id);
        Assert.Equal(new[] { "database" }, reloaded.Tags.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task RemoveTagFromAlertAsync_RemovesAssociation_ButKeepsTagRow()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var alert = await repository.AddAsync(NewAlert("Tagged"));
        await repository.AddTagsToAlertAsync(alert, new[] { "database", "prod" });

        var tracked = await repository.GetByIdAsync(alert.Id);
        var toRemove = tracked!.Tags.Single(t => t.Name == "database");
        await repository.RemoveTagFromAlertAsync(tracked, toRemove);

        context.ChangeTracker.Clear();
        var reloaded = await context.Alerts.Include(a => a.Tags).SingleAsync(a => a.Id == alert.Id);
        Assert.Equal(new[] { "prod" }, reloaded.Tags.Select(t => t.Name).ToArray());
        Assert.Equal(2, await context.Tags.CountAsync()); // shared tag row retained
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ReturnsCaseInsensitiveMatches()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var dbAlert = await repository.AddAsync(NewAlert("Has database tag", created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddTagsToAlertAsync(dbAlert, new[] { "Database" });
        var otherAlert = await repository.AddAsync(NewAlert("Has prod tag", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddTagsToAlertAsync(otherAlert, new[] { "prod" });

        var result = await repository.GetAllAsync(tag: "DATABASE");

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Has database tag", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilterAndOtherFilters_ReturnsOnlyAlertsMatchingAllCriteria()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var match = await repository.AddAsync(NewAlert("Disk prod active", Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await repository.AddTagsToAlertAsync(match, new[] { "prod" });
        var wrongTag = await repository.AddAsync(NewAlert("Disk staging active", Severity.Critical, new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        await repository.AddTagsToAlertAsync(wrongTag, new[] { "staging" });
        var wrongActive = await repository.AddAsync(NewAlert("Disk prod inactive", Severity.Critical, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await repository.AddTagsToAlertAsync(wrongActive, new[] { "prod" });

        var result = await repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            search: "disk",
            tag: "prod");

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Disk prod active", result.Items[0].Title);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTags()
    {
        var (connection, context, repository) = await CreateSqliteAsync();
        await using var _c = connection;
        await using var _ctx = context;

        var alert = await repository.AddAsync(NewAlert("Tagged"));
        await repository.AddTagsToAlertAsync(alert, new[] { "database", "prod" });
        context.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(alert.Id);

        Assert.NotNull(result);
        Assert.Equal(new[] { "database", "prod" }, result!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    // AC1: an active, same-severity, same-title (case-insensitive) alert inside the window is found, newest first.
    [Fact]
    public async Task FindActiveDuplicateAsync_WhenActiveMatchWithinWindow_ReturnsMostRecent()
    {
        var windowStart = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, created: windowStart.AddMinutes(5)));
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, created: windowStart.AddMinutes(10)));

        var result = await _repository.FindActiveDuplicateAsync("Disk full", Severity.High, windowStart);

        Assert.NotNull(result);
        Assert.Equal(windowStart.AddMinutes(10), result!.CreatedDate);
    }

    // AC1: title matching is case-insensitive.
    [Fact]
    public async Task FindActiveDuplicateAsync_MatchesTitleCaseInsensitively()
    {
        var windowStart = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Disk Full", Severity.High, created: windowStart.AddMinutes(5)));

        var result = await _repository.FindActiveDuplicateAsync("  disk full  ", Severity.High, windowStart);

        Assert.NotNull(result);
    }

    // AC5: a prior alert of a different severity is not a duplicate.
    [Fact]
    public async Task FindActiveDuplicateAsync_WhenDifferentSeverity_ReturnsNull()
    {
        var windowStart = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Disk full", Severity.Low, created: windowStart.AddMinutes(5)));

        var result = await _repository.FindActiveDuplicateAsync("Disk full", Severity.High, windowStart);

        Assert.Null(result);
    }

    // AC6: an inactive prior alert does not suppress.
    [Fact]
    public async Task FindActiveDuplicateAsync_WhenPriorIsInactive_ReturnsNull()
    {
        var windowStart = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, created: windowStart.AddMinutes(5), isActive: false));

        var result = await _repository.FindActiveDuplicateAsync("Disk full", Severity.High, windowStart);

        Assert.Null(result);
    }

    // Boundary: a match created before the window start is not returned.
    [Fact]
    public async Task FindActiveDuplicateAsync_WhenCreatedBeforeWindowStart_ReturnsNull()
    {
        var windowStart = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, created: windowStart.AddMinutes(-1)));

        var result = await _repository.FindActiveDuplicateAsync("Disk full", Severity.High, windowStart);

        Assert.Null(result);
    }

    private static async Task<(SqliteConnection Connection, AlertDbContext Context, AlertRepository Repository)> CreateSqliteAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AlertDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return (connection, context, new AlertRepository(context));
    }
}
