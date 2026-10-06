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

    private static readonly DateTime DupNow = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MatchesTitleCaseInsensitively_WithinWindow()
    {
        var added = await _repository.AddAsync(NewAlert("Disk Full", Severity.High, DupNow.AddMinutes(-5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("disk full", Severity.High, DupNow.AddMinutes(-15));

        Assert.NotNull(result);
        Assert.Equal(added.Id, result!.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_DifferentSeverity_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-5)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.Low, DupNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_InactiveAlert_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-5), isActive: false));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DupNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_OlderThanWindow_ReturnsNull()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-16)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DupNow.AddMinutes(-15));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_AtWindowBoundary_IsInclusive()
    {
        var added = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-15)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DupNow.AddMinutes(-15));

        Assert.Equal(added.Id, result?.Id);
    }

    [Fact]
    public async Task FindRecentActiveDuplicateAsync_MultipleMatches_ReturnsMostRecent()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-10)));
        var newest = await _repository.AddAsync(NewAlert("Disk full", Severity.High, DupNow.AddMinutes(-2)));

        var result = await _repository.FindRecentActiveDuplicateAsync("Disk full", Severity.High, DupNow.AddMinutes(-15));

        Assert.Equal(newest.Id, result!.Id);
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

    private async Task<Alert> AddAlertWithTagsAsync(string title, params string[] tags)
    {
        var alert = await _repository.AddAsync(NewAlert(title));
        if (tags.Length > 0)
        {
            await _repository.AddTagsAsync(alert, tags);
        }

        return alert;
    }

    [Fact]
    public async Task AddTagsAsync_PersistsTagsOnAlert()
    {
        var alert = await _repository.AddAsync(NewAlert());

        await _repository.AddTagsAsync(alert, new[] { "Prod", "db" });

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "db", "Prod" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(new[] { "db", "prod" }, reloaded.Tags.Select(t => t.NormalizedName).OrderBy(n => n));
    }

    [Fact]
    public async Task AddTagsAsync_StoresTrimmedNameWithNormalizedKey()
    {
        var alert = await _repository.AddAsync(NewAlert());

        await _repository.AddTagsAsync(alert, new[] { "  Prod  " });

        var tag = await _context.Tags.SingleAsync();
        Assert.Equal("Prod", tag.Name);
        Assert.Equal("prod", tag.NormalizedName);
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRow_AcrossAlertsCaseInsensitively()
    {
        var first = await AddAlertWithTagsAsync("First", "Prod");
        var second = await _repository.AddAsync(NewAlert("Second"));

        await _repository.AddTagsAsync(second, new[] { "PROD" });

        Assert.Equal(1, await _context.Tags.CountAsync());
        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(second.Id);
        Assert.Equal("Prod", reloaded!.Tags.Single().Name);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesAssignmentOnly_AndKeepsTagRow()
    {
        var alert = await AddAlertWithTagsAsync("Alert", "prod", "db");
        var prod = alert.Tags.Single(t => t.NormalizedName == "prod");

        await _repository.RemoveTagAsync(alert, prod);

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "db" }, reloaded!.Tags.Select(t => t.Name));
        Assert.Equal(2, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_DoesNotAffectOtherAlertsWithSameTag()
    {
        var first = await AddAlertWithTagsAsync("First", "prod");
        var second = await AddAlertWithTagsAsync("Second", "prod");

        await _repository.RemoveTagAsync(first, first.Tags.Single());

        _context.ChangeTracker.Clear();
        Assert.Empty((await _repository.GetByIdAsync(first.Id))!.Tags);
        Assert.Single((await _repository.GetByIdAsync(second.Id))!.Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsCarryingTag()
    {
        await AddAlertWithTagsAsync("Tagged", "prod");
        await AddAlertWithTagsAsync("Other tag", "db");
        await AddAlertWithTagsAsync("No tags");

        var result = await _repository.GetAllAsync(tag: "prod");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", result.Items.Single().Title);
    }

    [Theory]
    [InlineData("PROD")]
    [InlineData("prod")]
    [InlineData("  Prod  ")]
    public async Task GetAllAsync_WithTag_MatchesCaseInsensitivelyAndTrimmed(string filter)
    {
        await AddAlertWithTagsAsync("Tagged", "Prod");

        var result = await _repository.GetAllAsync(tag: filter);

        Assert.Equal("Tagged", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetAllAsync_WithUnknownTag_ReturnsEmpty()
    {
        await AddAlertWithTagsAsync("Tagged", "prod");

        var result = await _repository.GetAllAsync(tag: "missing");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithBlankTag_DoesNotFilter()
    {
        await AddAlertWithTagsAsync("Tagged", "prod");
        await AddAlertWithTagsAsync("Untagged");

        var result = await _repository.GetAllAsync(tag: "  ");

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsEachAlertOnce_WhenItHasMultipleTags()
    {
        await AddAlertWithTagsAsync("Multi", "prod", "db", "api");

        var result = await _repository.GetAllAsync(tag: "prod");

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].Tags.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ComposesWithOtherFilters()
    {
        async Task AddAsync(string title, Severity severity, DateTime created, bool active, params string[] tags)
        {
            var alert = await _repository.AddAsync(NewAlert(title, severity, created, active));
            await _repository.AddTagsAsync(alert, tags);
        }

        await AddAsync("Disk match", Severity.Critical, Jan.AddMonths(1), true, "prod");
        await AddAsync("Disk wrong tag", Severity.Critical, Jan.AddMonths(1), true, "db");
        await AddAsync("Disk inactive", Severity.Critical, Jan.AddMonths(1), false, "prod");
        await AddAsync("Disk wrong severity", Severity.Low, Jan.AddMonths(1), true, "prod");
        await AddAsync("Disk too old", Severity.Critical, Jan.AddDays(-30), true, "prod");
        await AddAsync("CPU wrong search", Severity.Critical, Jan.AddMonths(1), true, "prod");

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: Jan,
            createdTo: Jan.AddMonths(2),
            search: "disk",
            tag: "prod");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk match", result.Items.Single().Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_AppliesPagingSortingAndTotalCount()
    {
        await _repository.AddTagsAsync(await _repository.AddAsync(NewAlert("Charlie", created: Jan)), new[] { "prod" });
        await _repository.AddTagsAsync(await _repository.AddAsync(NewAlert("Alpha", created: Jan.AddDays(1))), new[] { "prod" });
        await _repository.AddTagsAsync(await _repository.AddAsync(NewAlert("Bravo", created: Jan.AddDays(2))), new[] { "prod" });
        await _repository.AddAsync(NewAlert("Untagged", created: Jan.AddDays(3)));

        var result = await _repository.GetAllAsync(tag: "prod", sortBy: "title", sortDirection: "asc", page: 2, pageSize: 2);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "Charlie" }, result.Items.Select(a => a.Title));
    }

    [Fact]
    public async Task GetAllAsync_LoadsTagsForEveryAlert()
    {
        await AddAlertWithTagsAsync("A", "prod");
        await AddAlertWithTagsAsync("B", "db", "api");
        _context.ChangeTracker.Clear();

        var result = await _repository.GetAllAsync();

        Assert.Equal(new[] { 1, 2 }, result.Items.Select(a => a.Tags.Count).OrderBy(c => c));
    }

    [Fact]
    public async Task UpdateAsync_KeepsTagAssignments()
    {
        var alert = await AddAlertWithTagsAsync("Alert", "prod");
        _context.ChangeTracker.Clear();

        var loaded = (await _repository.GetByIdAsync(alert.Id))!;
        loaded.Title = "Renamed";
        await _repository.UpdateAsync(loaded);

        _context.ChangeTracker.Clear();
        var reloaded = (await _repository.GetByIdAsync(alert.Id))!;
        Assert.Equal("Renamed", reloaded.Title);
        Assert.Equal("prod", reloaded.Tags.Single().Name);
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
    public async Task Model_PersistsAlertTagsJoinTable_AndDeletingAlertCascadesAssignments()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        var repository = new AlertRepository(context);
        var alert = await repository.AddAsync(NewAlert());
        await repository.AddTagsAsync(alert, new[] { "prod", "db" });

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM AlertTags";
            Assert.Equal(2L, await command.ExecuteScalarAsync());
        }

        await repository.DeleteAsync(alert);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM AlertTags";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
        }

        Assert.Equal(2, await context.Tags.CountAsync());
    }

    [Fact]
    public async Task Model_EnforcesUniqueNormalizedTagName()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        context.Tags.Add(new Tag { Name = "Prod", NormalizedName = "prod" });
        await context.SaveChangesAsync();

        context.Tags.Add(new Tag { Name = "PROD", NormalizedName = "prod" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task GetAllAsync_WithTag_MatchesCaseInsensitivelyUnderRelationalProvider()
    {
        var (connection, context) = await CreateSqliteContextAsync();
        await using var _c = connection;
        await using var _x = context;
        var repository = new AlertRepository(context);
        await repository.AddTagsAsync(await repository.AddAsync(NewAlert("Tagged")), new[] { "Prod" });
        await repository.AddAsync(NewAlert("Untagged"));

        var result = await repository.GetAllAsync(tag: "PROD");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", result.Items.Single().Title);
    }

    private sealed class SavingInterceptor : SaveChangesInterceptor
    {
        private readonly Func<int, Task> _onSaving;
        private int _calls;

        public SavingInterceptor(Func<int, Task> onSaving) => _onSaving = onSaving;

        public int Calls => _calls;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await _onSaving(_calls++);
            return result;
        }
    }

    private static async Task<SqliteConnection> CreateSharedSqliteAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var setup = CreateSharedContext(connection);
        await setup.Database.EnsureCreatedAsync();
        return connection;
    }

    private static AlertDbContext CreateSharedContext(SqliteConnection connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);

    [Fact]
    public async Task AddTagsAsync_WhenConcurrentWriterInsertsSameNewTag_RetriesAndReusesWinningTag()
    {
        await using var connection = await CreateSharedSqliteAsync();
        await using var seed = CreateSharedContext(connection);
        var alertId = (await new AlertRepository(seed).AddAsync(NewAlert())).Id;
        await using var writer = CreateSharedContext(connection);
        var interceptor = new SavingInterceptor(async call =>
        {
            if (call == 0)
            {
                writer.Tags.Add(new Tag { Name = "Prod", NormalizedName = "prod" });
                await writer.SaveChangesAsync();
            }
        });
        await using var context = CreateSharedContext(connection, interceptor);
        var repository = new AlertRepository(context);
        var alert = (await repository.GetByIdAsync(alertId))!;

        await repository.AddTagsAsync(alert, new[] { "prod" });

        await using var verify = CreateSharedContext(connection);
        Assert.Equal(1, await verify.Tags.CountAsync());
        var reloaded = await verify.Alerts.Include(a => a.Tags).SingleAsync();
        Assert.Equal("Prod", reloaded.Tags.Single().Name);
        Assert.Equal(2, interceptor.Calls);
    }

    [Fact]
    public async Task AddTagsAsync_WhenConcurrentWriterAssignsSameTagToAlert_RetryIsIdempotent()
    {
        await using var connection = await CreateSharedSqliteAsync();
        await using var seed = CreateSharedContext(connection);
        var alertId = (await new AlertRepository(seed).AddAsync(NewAlert())).Id;
        seed.Tags.Add(new Tag { Name = "prod", NormalizedName = "prod" });
        await seed.SaveChangesAsync();
        await using var writer = CreateSharedContext(connection);
        var interceptor = new SavingInterceptor(async call =>
        {
            if (call == 0)
            {
                var writerRepository = new AlertRepository(writer);
                await writerRepository.AddTagsAsync((await writerRepository.GetByIdAsync(alertId))!, new[] { "prod" });
            }
        });
        await using var context = CreateSharedContext(connection, interceptor);
        var repository = new AlertRepository(context);
        var alert = (await repository.GetByIdAsync(alertId))!;

        await repository.AddTagsAsync(alert, new[] { "prod" });

        await using var verify = CreateSharedContext(connection);
        var reloaded = await verify.Alerts.Include(a => a.Tags).SingleAsync();
        Assert.Equal("prod", reloaded.Tags.Single().Name);
        Assert.Equal(1, await verify.Tags.CountAsync());
        Assert.Equal("prod", alert.Tags.Single().Name);
    }

    [Fact]
    public async Task AddTagsAsync_WhenConflictPersistsAfterRetry_PropagatesDbUpdateException()
    {
        await using var connection = await CreateSharedSqliteAsync();
        await using var seed = CreateSharedContext(connection);
        var alertId = (await new AlertRepository(seed).AddAsync(NewAlert())).Id;
        var interceptor = new SavingInterceptor(_ => throw new DbUpdateException("conflict"));
        await using var context = CreateSharedContext(connection, interceptor);
        var repository = new AlertRepository(context);
        var alert = (await repository.GetByIdAsync(alertId))!;

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddTagsAsync(alert, new[] { "prod" }));

        Assert.Equal(2, interceptor.Calls);
    }
}
