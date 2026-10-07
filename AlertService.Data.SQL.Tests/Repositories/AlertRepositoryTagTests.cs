using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.Data.SQL.Repositories;
using AlertService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AlertService.Data.SQL.Tests.Repositories;

public class AlertRepositoryTagTests : IDisposable
{
    private readonly AlertDbContext _context;
    private readonly AlertRepository _repository;

    public AlertRepositoryTagTests()
    {
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

    private async Task<Alert> AddTaggedAlertAsync(string title, params string[] tags)
    {
        var alert = await _repository.AddAsync(NewAlert(title));
        if (tags.Length > 0)
        {
            await _repository.AddTagsAsync(alert, tags);
        }

        return alert;
    }

    [Fact]
    public async Task AddTagsAsync_PersistsTagsThroughManyToManyJoin_AndReloadsThemWithAlert()
    {
        var alert = await AddTaggedAlertAsync("Disk full", "disk", "storage");

        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new[] { "disk", "storage" }, reloaded!.Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRowSharedByTwoAlerts()
    {
        var first = await AddTaggedAlertAsync("First", "disk");
        var second = await AddTaggedAlertAsync("Second", "disk");

        _context.ChangeTracker.Clear();
        Assert.Equal(1, await _context.Tags.CountAsync());
        var tag = await _context.Tags.Include(t => t.Alerts).SingleAsync();
        Assert.Equal(new[] { first.Id, second.Id }, tag.Alerts.Select(a => a.Id).OrderBy(i => i).ToArray());
    }

    [Fact]
    public async Task AddTagsAsync_NormalizesNamesToTrimmedLowercase_AndDedupes()
    {
        var alert = await AddTaggedAlertAsync("Disk full", "  Disk ", "DISK");

        _context.ChangeTracker.Clear();
        var tags = await _context.Tags.ToListAsync();
        Assert.Equal("disk", Assert.Single(tags).Name);
        Assert.Single((await _repository.GetByIdAsync(alert.Id))!.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesAssignmentCaseInsensitively_AndKeepsOtherTags()
    {
        var alert = await AddTaggedAlertAsync("Disk full", "disk", "storage");

        var removed = await _repository.RemoveTagAsync(alert, " DISK ");

        Assert.True(removed);
        _context.ChangeTracker.Clear();
        var reloaded = await _repository.GetByIdAsync(alert.Id);
        Assert.Equal(new[] { "storage" }, reloaded!.Tags.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse()
    {
        var alert = await AddTaggedAlertAsync("Disk full", "disk");

        var removed = await _repository.RemoveTagAsync(alert, "cpu");

        Assert.False(removed);
        Assert.Single(alert.Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_ReturnsOnlyAlertsWithThatTagCaseInsensitively()
    {
        await AddTaggedAlertAsync("Tagged disk", "disk", "storage");
        await AddTaggedAlertAsync("Tagged cpu", "cpu");
        await AddTaggedAlertAsync("Untagged");

        var result = await _repository.GetAllAsync(new AlertQueryOptions { Tag = "  DISK " });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Tagged disk", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_WithTagNoAlertHas_ReturnsEmptyResult()
    {
        await AddTaggedAlertAsync("Tagged disk", "disk");

        var result = await _repository.GetAllAsync(new AlertQueryOptions { Tag = "missing" });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_IncludesAllTagsOfMatchingAlerts_NotOnlyTheFilteredTag()
    {
        await AddTaggedAlertAsync("Tagged disk", "disk", "storage");

        var result = await _repository.GetAllAsync(new AlertQueryOptions { Tag = "disk" });

        Assert.Equal(new[] { "disk", "storage" }, Assert.Single(result.Items).Tags.Select(t => t.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithTagAndOtherFilters_ReturnsOnlyAlertsMatchingAllCriteria()
    {
        var match = await _repository.AddAsync(NewAlert("Disk match", Severity.Critical, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongSeverity = await _repository.AddAsync(NewAlert("Disk severity", Severity.Low, new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var inactive = await _repository.AddAsync(NewAlert("Disk inactive", Severity.Critical, new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), isActive: false));
        var outOfRange = await _repository.AddAsync(NewAlert("Disk range", Severity.Critical, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongSearch = await _repository.AddAsync(NewAlert("CPU spike", Severity.Critical, new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        var wrongTag = await _repository.AddAsync(NewAlert("Disk other tag", Severity.Critical, new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), isActive: true));
        foreach (var alert in new[] { match, wrongSeverity, inactive, outOfRange, wrongSearch })
        {
            await _repository.AddTagsAsync(alert, new[] { "ops" });
        }

        await _repository.AddTagsAsync(wrongTag, new[] { "other" });

        var result = await _repository.GetAllAsync(new AlertQueryOptions
        {
            IsActive = true,
            Severity = Severity.Critical,
            CreatedFrom = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedTo = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
            Search = "disk",
            Tag = "ops"
        });

        Assert.Equal("Disk match", Assert.Single(result.Items).Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithTagUnderRelationalQuery_FiltersThroughJoinTable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AlertDbContext>().UseSqlite(connection).Options;
        await using var context = new AlertDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new AlertRepository(context);

        var tagged = await repository.AddAsync(NewAlert("Tagged"));
        await repository.AddAsync(NewAlert("Untagged"));
        await repository.AddTagsAsync(tagged, new[] { "Disk" });

        var result = await repository.GetAllAsync(new AlertQueryOptions { Tag = "disk" });

        Assert.Equal("Tagged", Assert.Single(result.Items).Title);
        Assert.Equal(1, result.TotalCount);
    }
}
