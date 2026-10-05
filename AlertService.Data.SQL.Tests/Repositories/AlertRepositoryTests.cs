using AlertService.Common.Enums;
using AlertService.Data.SQL.Repositories;
using AlertService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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

    private static readonly DateTime Jan = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Feb = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Mar = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private async Task<Alert> AddTaggedAlertAsync(string title, Severity severity, DateTime created, bool isActive, params string[] tags)
    {
        var alert = await _repository.AddAsync(NewAlert(title, severity, created, isActive));
        if (tags.Length > 0)
        {
            await _repository.AddTagsAsync(alert, tags);
        }

        return alert;
    }

    private static async Task<(SqliteConnection Connection, AlertDbContext Context)> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var context = new AlertDbContext(new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        return (connection, context);
    }

    [Fact]
    public void Model_MapsTagAsManyToManyWithAlertThroughAlertTagsJoinTable()
    {
        var tagType = _context.Model.FindEntityType(typeof(Tag))!;
        var skip = Assert.Single(tagType.GetSkipNavigations());

        Assert.Equal(nameof(Tag.Alerts), skip.Name);
        Assert.Equal("AlertTags", skip.JoinEntityType.Name);
        Assert.Equal(nameof(Alert.Tags), skip.Inverse!.Name);
    }

    [Fact]
    public void Model_ConfiguresUniqueIndexOnTagName()
    {
        var tagType = _context.Model.FindEntityType(typeof(Tag))!;

        var index = Assert.Single(tagType.GetIndexes(), i => i.Properties.Single().Name == nameof(Tag.Name));
        Assert.True(index.IsUnique);
        Assert.Equal(30, tagType.FindProperty(nameof(Tag.Name))!.GetMaxLength());
    }

    [Fact]
    public async Task Migrations_IncludeAddAlertTagsMigration()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;

        var migrations = context.GetService<IMigrationsAssembly>().Migrations.Keys;

        Assert.Contains(migrations, m => m.EndsWith("_AddAlertTags", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddTagsAsync_PersistsTagsOnAlert()
    {
        var alert = await _repository.AddAsync(NewAlert());

        await _repository.AddTagsAsync(alert, new[] { "ops", "prod" });

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "ops", "prod" }, reloaded!.Tags.Select(t => t.Name).Order().ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagCaseInsensitivelyAcrossAlerts()
    {
        await AddTaggedAlertAsync("First", Severity.Low, Jan, true, "Prod");
        var second = await _repository.AddAsync(NewAlert("Second", created: Feb));

        await _repository.AddTagsAsync(second, new[] { "prod" });

        Assert.Equal(1, await _context.Tags.CountAsync());
        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(second.Id);
        Assert.Equal("Prod", Assert.Single(reloaded!.Tags).Name);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_RemovesAssignmentCaseInsensitively_AndKeepsTagEntity()
    {
        var alert = await AddTaggedAlertAsync("Disk", Severity.High, Jan, true, "Prod", "ops");

        var removed = await _repository.RemoveTagAsync(alert, "prod");

        Assert.True(removed);
        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "ops" }, reloaded!.Tags.Select(t => t.Name).ToArray());
        Assert.Equal(2, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenNotAssigned_ReturnsFalse_AndChangesNothing()
    {
        var alert = await AddTaggedAlertAsync("Disk", Severity.High, Jan, true, "ops");

        var removed = await _repository.RemoveTagAsync(alert, "prod");

        Assert.False(removed);
        _context.ChangeTracker.Clear();
        Assert.Single((await _repository.GetByIdAsync(alert.Id))!.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_OnlyAffectsTheGivenAlert()
    {
        var a = await AddTaggedAlertAsync("A", Severity.High, Jan, true, "ops");
        var b = await AddTaggedAlertAsync("B", Severity.High, Feb, true, "ops");

        await _repository.RemoveTagAsync(a, "ops");

        _context.ChangeTracker.Clear();
        Assert.Empty((await _repository.GetByIdAsync(a.Id))!.Tags);
        Assert.Single((await _repository.GetByIdAsync(b.Id))!.Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsHavingThatTag_CaseInsensitively()
    {
        await AddTaggedAlertAsync("Tagged", Severity.High, Jan, true, "Prod", "ops");
        await AddTaggedAlertAsync("Other tag", Severity.High, Feb, true, "dev");
        await AddTaggedAlertAsync("Untagged", Severity.High, Mar, true);

        var result = await _repository.GetAllAsync(tag: "pROD");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_TrimsFilterValue()
    {
        await AddTaggedAlertAsync("Tagged", Severity.High, Jan, true, "prod");

        var result = await _repository.GetAllAsync(tag: "  prod ");

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithUnknownTag_ReturnsEmptyPage()
    {
        await AddTaggedAlertAsync("Tagged", Severity.High, Jan, true, "prod");

        var result = await _repository.GetAllAsync(tag: "nope");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_WithBlankTag_DoesNotFilter(string? tag)
    {
        await AddTaggedAlertAsync("Tagged", Severity.High, Jan, true, "prod");
        await AddTaggedAlertAsync("Untagged", Severity.High, Feb, true);

        var result = await _repository.GetAllAsync(tag: tag);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAlertsWithTagsLoaded_EmptyWhenNone()
    {
        await AddTaggedAlertAsync("Tagged", Severity.High, Feb, true, "ops", "prod");
        await AddTaggedAlertAsync("Untagged", Severity.High, Jan, true);
        _context.ChangeTracker.Clear();

        var result = await _repository.GetAllAsync();

        Assert.Equal(new[] { "ops", "prod" }, result.Items[0].Tags.Select(t => t.Name).Order().ToArray());
        Assert.Empty(result.Items[1].Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndIsActive_ComposesWithAnd()
    {
        await AddTaggedAlertAsync("Active tagged", Severity.High, Jan, true, "ops");
        await AddTaggedAlertAsync("Inactive tagged", Severity.High, Feb, false, "ops");
        await AddTaggedAlertAsync("Active untagged", Severity.High, Mar, true);

        var result = await _repository.GetAllAsync(isActive: true, tag: "ops");

        Assert.Equal("Active tagged", Assert.Single(result.Items).Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndSeverity_ComposesWithAnd()
    {
        await AddTaggedAlertAsync("Critical tagged", Severity.Critical, Jan, true, "ops");
        await AddTaggedAlertAsync("Low tagged", Severity.Low, Feb, true, "ops");
        await AddTaggedAlertAsync("Critical other", Severity.Critical, Mar, true, "dev");

        var result = await _repository.GetAllAsync(severity: Severity.Critical, tag: "ops");

        Assert.Equal("Critical tagged", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndCreatedRange_ComposesWithAnd()
    {
        await AddTaggedAlertAsync("Too old", Severity.High, Jan, true, "ops");
        await AddTaggedAlertAsync("In range", Severity.High, Feb, true, "ops");
        await AddTaggedAlertAsync("Too new", Severity.High, Mar, true, "ops");
        await AddTaggedAlertAsync("In range other tag", Severity.High, Feb.AddDays(1), true, "dev");

        var result = await _repository.GetAllAsync(createdFrom: Feb, createdTo: Feb.AddDays(10), tag: "ops");

        Assert.Equal("In range", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndSearch_ComposesWithAnd()
    {
        await AddTaggedAlertAsync("Disk full", Severity.High, Jan, true, "ops");
        await AddTaggedAlertAsync("CPU spike", Severity.High, Feb, true, "ops");
        await AddTaggedAlertAsync("Disk slow", Severity.High, Mar, true, "dev");

        var result = await _repository.GetAllAsync(search: "disk", tag: "ops");

        Assert.Equal("Disk full", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_AppliesPagingSortingAndTotalCount()
    {
        await AddTaggedAlertAsync("Charlie", Severity.High, Jan, true, "ops");
        await AddTaggedAlertAsync("Alpha", Severity.High, Feb, true, "ops");
        await AddTaggedAlertAsync("Bravo", Severity.High, Mar, true, "ops");
        await AddTaggedAlertAsync("Zulu", Severity.High, Mar, true, "dev");

        var result = await _repository.GetAllAsync(sortBy: "title", sortDirection: "asc", page: 2, pageSize: 2, tag: "ops");

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "Charlie" }, result.Items.Select(a => a.Title).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithTag_WhenAlertHasSeveralTags_DoesNotDuplicateAlert()
    {
        await AddTaggedAlertAsync("Multi", Severity.High, Jan, true, "ops", "Ops2", "prod");

        var result = await _repository.GetAllAsync(tag: "ops");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(3, Assert.Single(result.Items).Tags.Count);
    }

    [Fact]
    public async Task Sqlite_GetAllAsync_WithTagAndSeverity_TranslatesAndFilters()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        var repository = new AlertRepository(context);
        var critical = await repository.AddAsync(NewAlert("Critical tagged", Severity.Critical, Jan));
        var low = await repository.AddAsync(NewAlert("Low tagged", Severity.Low, Feb));
        await repository.AddTagsAsync(critical, new[] { "Ops" });
        await repository.AddTagsAsync(low, new[] { "ops" });

        var result = await repository.GetAllAsync(severity: Severity.Critical, tag: "OPS");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Critical tagged", Assert.Single(result.Items).Title);
        Assert.Equal("Ops", Assert.Single(result.Items[0].Tags).Name);
    }

    [Fact]
    public async Task Sqlite_TagNameUniqueIndex_RejectsDuplicateNames()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        context.Tags.Add(new Tag { Name = "ops" });
        await context.SaveChangesAsync();

        context.Tags.Add(new Tag { Name = "ops" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_DeleteAlert_RemovesJoinRows_ButKeepsTags()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        var repository = new AlertRepository(context);
        var alert = await repository.AddAsync(NewAlert());
        await repository.AddTagsAsync(alert, new[] { "ops", "prod" });
        var loaded = (await repository.GetByIdAsync(alert.Id))!;

        await repository.DeleteAsync(loaded);

        var joinRows = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AlertTags").SingleAsync();
        Assert.Equal(0, joinRows);
        Assert.Equal(2, await context.Tags.CountAsync());
    }
}
