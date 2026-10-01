using AlertService.Common.Constants;
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
    public async Task AddTagsAsync_PersistsTags_AndDeduplicatesCaseInsensitiveAssignments()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));
        var trackedAlert = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(trackedAlert);

        await _repository.AddTagsAsync(trackedAlert!, ["ops", " Ops ", "database"]);

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(["database", "ops"], reloaded!.AlertTags.Select(alertTag => alertTag.Tag.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
        Assert.Equal(2, await _context.Tags.CountAsync());
        Assert.Equal(2, await _context.AlertTags.CountAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesAssignment_AndLeavesTagRecord()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));
        var trackedAlert = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(trackedAlert);

        await _repository.AddTagsAsync(trackedAlert!, ["ops"]);
        await _repository.RemoveTagAsync(trackedAlert!, "ops");

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(reloaded);
        Assert.Empty(reloaded!.AlertTags);
        Assert.Equal(1, await _context.Tags.CountAsync());
        Assert.Equal(0, await _context.AlertTags.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ComposesWithExistingFilters()
    {
        var matching = await _repository.AddAsync(NewAlert("Disk critical", Severity.Critical, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var inactive = await _repository.AddAsync(NewAlert("Disk inactive", Severity.Critical, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        var wrongSeverity = await _repository.AddAsync(NewAlert("Disk warning", Severity.High, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongTitle = await _repository.AddAsync(NewAlert("CPU critical", Severity.Critical, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: true));

        foreach (var alertId in new[] { matching.Id, inactive.Id, wrongSeverity.Id, wrongTitle.Id })
        {
            var trackedAlert = await _repository.GetByIdAsync(alertId);
            Assert.NotNull(trackedAlert);
            await _repository.AddTagsAsync(trackedAlert!, ["ops"]);
        }

        var otherAlert = await _repository.AddAsync(NewAlert("Disk different tag", Severity.Critical, new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var otherTrackedAlert = await _repository.GetByIdAsync(otherAlert.Id);
        Assert.NotNull(otherTrackedAlert);
        await _repository.AddTagsAsync(otherTrackedAlert!, ["database"]);

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            search: "disk",
            tag: "OPS",
            page: 1,
            pageSize: AlertConstants.DefaultPageSize);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Disk critical", result.Items[0].Title);
        Assert.Equal(["ops"], result.Items[0].AlertTags.Select(alertTag => alertTag.Tag.Name).ToArray());
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
    public async Task GetDailyTrendsAsync_ReturnsOldestFirstBuckets_WithUtcDateWindowAndSeverityCounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AlertDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new AlertRepository(context);

        await repository.AddAsync(NewAlert("Before window", Severity.Low, new DateTime(2026, 2, 27, 23, 59, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Day one low", Severity.Low, new DateTime(2026, 2, 28, 1, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Day one critical", Severity.Critical, new DateTime(2026, 2, 28, 22, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Day two high a", Severity.High, new DateTime(2026, 3, 1, 2, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("Day two high b", Severity.High, new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(NewAlert("At exclusive end", Severity.Medium, new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)));

        var result = await repository.GetDailyTrendsAsync(
            new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), first.Date);
                Assert.Equal(2, first.TotalCount);
                Assert.Equal(1, first.LowCount);
                Assert.Equal(0, first.MediumCount);
                Assert.Equal(0, first.HighCount);
                Assert.Equal(1, first.CriticalCount);
            },
            second =>
            {
                Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), second.Date);
                Assert.Equal(2, second.TotalCount);
                Assert.Equal(0, second.LowCount);
                Assert.Equal(0, second.MediumCount);
                Assert.Equal(2, second.HighCount);
                Assert.Equal(0, second.CriticalCount);
            });
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
    public async Task GetActiveDuplicateAsync_ReturnsNewestActiveMatchingAlertWithinWindow()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, new DateTime(2026, 1, 1, 11, 40, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("disk full", Severity.High, new DateTime(2026, 1, 1, 11, 50, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, new DateTime(2026, 1, 1, 11, 30, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Disk full", Severity.Critical, new DateTime(2026, 1, 1, 11, 55, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetActiveDuplicateAsync(
            "  DISK FULL  ",
            Severity.High,
            new DateTime(2026, 1, 1, 11, 45, 0, DateTimeKind.Utc));

        Assert.NotNull(result);
        Assert.Equal("disk full", result!.Title);
        Assert.Equal(Severity.High, result.Severity);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetActiveDuplicateAsync_IgnoresAlertsOutsideWindow_InactiveAlerts_AndDifferentSeverity()
    {
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, new DateTime(2026, 1, 1, 11, 40, 0, DateTimeKind.Utc), isActive: true));
        await _repository.AddAsync(NewAlert("Disk full", Severity.High, new DateTime(2026, 1, 1, 11, 50, 0, DateTimeKind.Utc), isActive: false));
        await _repository.AddAsync(NewAlert("Disk full", Severity.Critical, new DateTime(2026, 1, 1, 11, 55, 0, DateTimeKind.Utc), isActive: true));

        var result = await _repository.GetActiveDuplicateAsync(
            "Disk full",
            Severity.High,
            new DateTime(2026, 1, 1, 11, 45, 0, DateTimeKind.Utc));

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
}
