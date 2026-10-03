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
    public async Task GetDailyTrendsAsync_GroupsByUtcDayAndSeverity_WithinRange()
    {
        // Day A has two Low and one High (across different times of the same UTC day); Day B has one Critical.
        await _repository.AddAsync(NewAlert("Low A1", Severity.Low, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Low A2", Severity.Low, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("High A", Severity.High, new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("Critical B", Severity.Critical, new DateTime(2026, 1, 2, 6, 0, 0, DateTimeKind.Utc)));
        // Out of range: before fromInclusive and on/after toExclusive.
        await _repository.AddAsync(NewAlert("Before range", Severity.Medium, new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc)));
        await _repository.AddAsync(NewAlert("At toExclusive", Severity.Low, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailyTrendsAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var ordered = result.OrderBy(r => r.Day).ToList();
        Assert.Equal(2, ordered.Count);

        Assert.Equal(new DateTime(2026, 1, 1), ordered[0].Day);
        Assert.Equal(2, ordered[0].LowCount);
        Assert.Equal(0, ordered[0].MediumCount);
        Assert.Equal(1, ordered[0].HighCount);
        Assert.Equal(0, ordered[0].CriticalCount);

        Assert.Equal(new DateTime(2026, 1, 2), ordered[1].Day);
        Assert.Equal(0, ordered[1].LowCount);
        Assert.Equal(0, ordered[1].MediumCount);
        Assert.Equal(0, ordered[1].HighCount);
        Assert.Equal(1, ordered[1].CriticalCount);
    }

    [Fact]
    public async Task GetDailyTrendsAsync_WhenNoAlertsInRange_ReturnsEmpty()
    {
        await _repository.AddAsync(NewAlert("Outside", Severity.High, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await _repository.GetDailyTrendsAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc));

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
    public async Task AddTagsToAlertAsync_CreatesNewTagRows_AndAttachesThemToAlert()
    {
        var alert = await _repository.AddAsync(NewAlert());

        await _repository.AddTagsToAlertAsync(alert, new[] { "database", "cache" });

        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alerts.Include(a => a.Tags).SingleAsync(a => a.Id == alert.Id);
        Assert.Equal(new[] { "cache", "database" }, reloaded.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Equal(2, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task AddTagsToAlertAsync_ReusesExistingTagRow_CaseInsensitively()
    {
        var alert1 = await _repository.AddAsync(NewAlert("Alert one"));
        var alert2 = await _repository.AddAsync(NewAlert("Alert two"));

        await _repository.AddTagsToAlertAsync(alert1, new[] { "database" });
        await _repository.AddTagsToAlertAsync(alert2, new[] { "DATABASE" });

        Assert.Equal(1, await _context.Tags.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ReturnsOnlyMatchingAlerts_CaseInsensitive()
    {
        var tagged = await _repository.AddAsync(NewAlert("Tagged", created: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        var other = await _repository.AddAsync(NewAlert("Untagged", created: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsToAlertAsync(tagged, new[] { "Production" });
        await _repository.AddTagsToAlertAsync(other, new[] { "staging" });

        var result = await _repository.GetAllAsync(tag: "PRODUCTION");

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ComposesWithOtherFilters()
    {
        var match = await _repository.AddAsync(NewAlert("Match", Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongSeverity = await _repository.AddAsync(NewAlert("Wrong severity", Severity.High, new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongActive = await _repository.AddAsync(NewAlert("Wrong active", Severity.Critical, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddTagsToAlertAsync(match, new[] { "prod" });
        await _repository.AddTagsToAlertAsync(wrongSeverity, new[] { "prod" });
        await _repository.AddTagsToAlertAsync(wrongActive, new[] { "prod" });

        var result = await _repository.GetAllAsync(isActive: true, severity: Severity.Critical, tag: "prod");

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Match", result.Items[0].Title);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTags()
    {
        var alert = await _repository.AddAsync(NewAlert());
        await _repository.AddTagsToAlertAsync(alert, new[] { "database" });

        _context.ChangeTracker.Clear();
        var result = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(result);
        Assert.Contains(result!.Tags, t => t.Name == "database");
    }

    // AC1: active + same title/severity + CreatedDate within window returns the match.
    [Fact]
    public async Task FindActiveDuplicateAsync_ReturnsMatch_WhenActiveSameTitleSeverityWithinWindow()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.FindActiveDuplicateAsync("Service down", Severity.Critical, createdOnOrAfter);

        Assert.NotNull(result);
        Assert.Equal("Service down", result!.Title);
        Assert.Equal(Severity.Critical, result.Severity);
    }

    // AC1: title match is case-insensitive and title is trimmed before matching.
    [Fact]
    public async Task FindActiveDuplicateAsync_MatchesTitleCaseInsensitively_AndTrimsInput()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service Down", Severity.High, created: new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.FindActiveDuplicateAsync("  service down  ", Severity.High, createdOnOrAfter);

        Assert.NotNull(result);
        Assert.Equal("Service Down", result!.Title);
    }

    // AC4: a prior alert with a different severity is never a duplicate.
    [Fact]
    public async Task FindActiveDuplicateAsync_ReturnsNull_WhenDifferentSeverity()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service down", Severity.High, created: new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.FindActiveDuplicateAsync("Service down", Severity.Critical, createdOnOrAfter);

        Assert.Null(result);
    }

    // AC4: an inactive prior alert is never a duplicate.
    [Fact]
    public async Task FindActiveDuplicateAsync_ReturnsNull_WhenInactive()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), isActive: false));

        var result = await _repository.FindActiveDuplicateAsync("Service down", Severity.Critical, createdOnOrAfter);

        Assert.Null(result);
    }

    // AC1: an alert created before the window start is not a duplicate.
    [Fact]
    public async Task FindActiveDuplicateAsync_ReturnsNull_WhenOutsideWindow()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 11, 59, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.FindActiveDuplicateAsync("Service down", Severity.Critical, createdOnOrAfter);

        Assert.Null(result);
    }

    // AC1: when multiple active duplicates exist, the most recent by CreatedDate is returned.
    [Fact]
    public async Task FindActiveDuplicateAsync_ReturnsMostRecent_WhenMultipleMatches()
    {
        var createdOnOrAfter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 12, 3, 0, DateTimeKind.Utc), isActive: true));
        var newest = await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 12, 8, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Service down", Severity.Critical, created: new DateTime(2026, 1, 1, 12, 1, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.FindActiveDuplicateAsync("Service down", Severity.Critical, createdOnOrAfter);

        Assert.NotNull(result);
        Assert.Equal(newest.Id, result!.Id);
    }
}
