using AlertService.Common.Enums;
using AlertService.Data.SQL.Repositories;
using AlertService.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertService.Data.SQL.Tests.Repositories;

public class AlertRepositoryTagTests : IDisposable
{
    private readonly AlertDbContext _context;
    private readonly AlertRepository _repository;

    public AlertRepositoryTagTests()
    {
        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseInMemoryDatabase($"AlertTagDb_{Guid.NewGuid()}")
            .Options;

        _context = new AlertDbContext(options);
        _repository = new AlertRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static Alert NewAlert(
        string title,
        Severity severity = Severity.High,
        DateTime? created = null,
        bool isActive = true)
    {
        return new Alert
        {
            Title = title,
            Description = "Test description",
            Severity = severity,
            CreatedDate = created ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = isActive
        };
    }

    [Fact]
    public async Task AddTagsAsync_PersistsTags_AndReloadsAlertWithAssignedTags()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));

        await _repository.AddTagsAsync(alert, ["Ops", "Database"]);
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(alert.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(2, await _context.Tags.CountAsync());
        Assert.Equal(["Database", "Ops"], reloaded!.Tags.Select(tag => tag.Name).OrderBy(name => name).ToArray());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_RemovesTagFromAlert()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));
        await _repository.AddTagsAsync(alert, ["Ops"]);

        var trackedAlert = await _repository.GetByIdAsync(alert.Id);
        var removed = await _repository.RemoveTagAsync(trackedAlert!, "OPS");
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(alert.Id);

        Assert.True(removed);
        Assert.NotNull(reloaded);
        Assert.Empty(reloaded!.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsFalse()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));
        await _repository.AddTagsAsync(alert, ["Ops"]);

        var trackedAlert = await _repository.GetByIdAsync(alert.Id);
        var removed = await _repository.RemoveTagAsync(trackedAlert!, "DATABASE");

        Assert.False(removed);
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ComposesWithExistingFilters_AndPreservesTotalCount()
    {
        var matchingNewest = await _repository.AddAsync(NewAlert(
            "Disk alert newest",
            Severity.Critical,
            new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsAsync(matchingNewest, ["Ops"]);

        var matchingOlder = await _repository.AddAsync(NewAlert(
            "Disk alert older",
            Severity.Critical,
            new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsAsync(matchingOlder, ["OPS"]);

        var wrongTag = await _repository.AddAsync(NewAlert(
            "Disk alert other tag",
            Severity.Critical,
            new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsAsync(wrongTag, ["Billing"]);

        var wrongSeverity = await _repository.AddAsync(NewAlert(
            "Disk alert lower severity",
            Severity.High,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsAsync(wrongSeverity, ["Ops"]);

        var wrongSearch = await _repository.AddAsync(NewAlert(
            "Cpu alert",
            Severity.Critical,
            new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc)));
        await _repository.AddTagsAsync(wrongSearch, ["Ops"]);

        var result = await _repository.GetAllAsync(
            isActive: true,
            severity: Severity.Critical,
            createdFrom: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdTo: new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            tag: "ops",
            search: "disk",
            page: 1,
            pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Disk alert newest", result.Items[0].Title);
        Assert.Equal(["Ops"], result.Items[0].Tags.Select(tag => tag.Name).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_WithTagFilter_MatchesCaseInsensitively()
    {
        var alert = await _repository.AddAsync(NewAlert("Disk full"));
        await _repository.AddTagsAsync(alert, ["Ops"]);

        var result = await _repository.GetAllAsync(tag: "oPs");

        Assert.Single(result.Items);
        Assert.Equal(alert.Id, result.Items[0].Id);
    }
}