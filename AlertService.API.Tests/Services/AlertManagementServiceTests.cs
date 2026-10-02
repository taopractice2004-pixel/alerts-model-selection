using AlertService.API.Services;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AlertService.API.Tests.Services;

public class AlertManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private const int WindowMinutes = 15;

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = CreateService(WindowMinutes);
    }

    private AlertManagementService CreateService(int windowMinutes) => new(
        _repository.Object,
        _timeProvider.Object,
        Options.Create(new AlertSuppressionOptions { DuplicateWindowMinutes = windowMinutes }),
        NullLogger<AlertManagementService>.Instance);

    private static Alert ExistingAlert(int id = 1) => new()
    {
        Id = id,
        Title = "Memory leak",
        Description = "Heap growing",
        Severity = Severity.Medium,
        CreatedDate = FixedNow.UtcDateTime.AddDays(-1),
        IsActive = true
    };

    [Fact]
    public async Task GetAllAsync_MapsEntitiesToPagedResponse()
    {
        _repository.Setup(r => r.GetAllAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                "createdDate",
                "desc",
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1), ExistingAlert(2) }, 2));

        var result = await _service.GetAllAsync(new AlertQueryRequest());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new[] { 1, 2 }, result.Items.Select(r => r.Id));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_PassesQueryOptionsToRepository()
    {
        var request = new AlertQueryRequest
        {
            IsActive = true,
            Severity = Severity.Critical,
            CreatedFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedTo = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Search = "disk",
            Tag = "Disk",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "Disk", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "Disk", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsResponse()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Memory leak", result!.Title);
        Assert.Equal(Severity.Medium, result.Severity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.GetByIdAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSummaryAsync_MapsRepositoryCountsToDto()
    {
        _repository.Setup(r => r.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((6, 4, 2, 1, 2, 1, 2));

        var result = await _service.GetSummaryAsync();

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(4, result.ActiveCount);
        Assert.Equal(2, result.InactiveCount);
        Assert.Equal(1, result.SeverityCounts.Low);
        Assert.Equal(2, result.SeverityCounts.Medium);
        Assert.Equal(1, result.SeverityCounts.High);
        Assert.Equal(2, result.SeverityCounts.Critical);
        _repository.Verify(r => r.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupDailyCounts(params (DateTime Date, Severity Severity, int Count)[] counts)
    {
        IReadOnlyList<(DateTime Date, Severity Severity, int Count)> result = counts;
        _repository.Setup(r => r.GetDailySeverityCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    [Fact]
    public async Task GetTrendsAsync_ReturnsOneBucketPerDay_OldestFirst_EndingToday()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        Assert.Equal(3, result.Days);
        Assert.Equal(
            new[] { new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1) },
            result.Buckets.Select(b => b.Date));
    }

    [Fact]
    public async Task GetTrendsAsync_QueriesRepositoryFromFirstDayInclusiveToTomorrowExclusive()
    {
        SetupDailyCounts();

        await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        _repository.Verify(r => r.GetDailySeverityCountsAsync(
            new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrendsAsync_ZeroFillsMissingDaysAndSeverities_AndMapsCounts()
    {
        SetupDailyCounts(
            (new DateTime(2026, 8, 31), Severity.High, 2),
            (new DateTime(2026, 8, 31), Severity.Critical, 1),
            (new DateTime(2026, 9, 1), Severity.Low, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        var empty = result.Buckets[0];
        Assert.Equal(0, empty.TotalCount);
        Assert.Equal(0, empty.SeverityCounts.Low + empty.SeverityCounts.Medium + empty.SeverityCounts.High + empty.SeverityCounts.Critical);

        var middle = result.Buckets[1];
        Assert.Equal(3, middle.TotalCount);
        Assert.Equal(0, middle.SeverityCounts.Low);
        Assert.Equal(0, middle.SeverityCounts.Medium);
        Assert.Equal(2, middle.SeverityCounts.High);
        Assert.Equal(1, middle.SeverityCounts.Critical);

        var today = result.Buckets[2];
        Assert.Equal(4, today.TotalCount);
        Assert.Equal(4, today.SeverityCounts.Low);
        Assert.Equal(0, today.SeverityCounts.Medium);
        Assert.Equal(0, today.SeverityCounts.High);
        Assert.Equal(0, today.SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_MapsEachSeverityToItsOwnField()
    {
        var day = new DateTime(2026, 9, 1);
        SetupDailyCounts((day, Severity.Low, 1), (day, Severity.Medium, 2), (day, Severity.High, 3), (day, Severity.Critical, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(10, bucket.TotalCount);
        Assert.Equal(1, bucket.SeverityCounts.Low);
        Assert.Equal(2, bucket.SeverityCounts.Medium);
        Assert.Equal(3, bucket.SeverityCounts.High);
        Assert.Equal(4, bucket.SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_SumsMultipleGroupsForSameDayAndSeverity()
    {
        var day = new DateTime(2026, 9, 1);
        SetupDailyCounts((day, Severity.High, 2), (day, Severity.High, 3));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(5, bucket.TotalCount);
        Assert.Equal(5, bucket.SeverityCounts.High);
    }

    [Fact]
    public async Task GetTrendsAsync_WithDefaultRequest_ReturnsSevenBuckets()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest());

        Assert.Equal(7, result.Days);
        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(new DateOnly(2026, 8, 26), result.Buckets[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    [Fact]
    public async Task GetTrendsAsync_WithOneDay_ReturnsOnlyToday()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(new DateOnly(2026, 9, 1), bucket.Date);
    }

    [Fact]
    public async Task GetTrendsAsync_WithMaxDays_ReturnsNinetyBuckets()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 90 });

        Assert.Equal(90, result.Buckets.Count);
        Assert.Equal(new DateOnly(2026, 6, 4), result.Buckets[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    [Fact]
    public async Task GetTrendsAsync_WhenRequestIsNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetTrendsAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => { saved = a; a.Id = 10; })
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var request = new CreateAlertRequest
        {
            Title = "  Service down  ",
            Description = " Payments API ",
            Severity = Severity.Critical,
            IsActive = true
        };

        var result = await _service.CreateAsync(request);

        Assert.NotNull(saved);
        Assert.Equal("Service down", saved!.Title);
        Assert.Equal("Payments API", saved.Description);
        Assert.Equal(FixedNow.UtcDateTime, saved.CreatedDate);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateWithinWindow_ReturnsExistingWithoutAdding()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "memory leak", Severity = Severity.Medium });

        Assert.True(result.DuplicateSuppressed);
        Assert.Equal(7, result.Alert.Id);
        Assert.Equal("Memory leak", result.Alert.Title);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NoDuplicate_CreatesAlert()
    {
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 11)
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        Assert.False(result.DuplicateSuppressed);
        Assert.Equal(11, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredWindow_TrimmedTitleAndSeverity()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await _service.CreateAsync(new CreateAlertRequest { Title = "  Service down  ", Severity = Severity.Critical });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Service down",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-WindowMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesWindowFromOptions()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);
        var service = CreateService(5);

        await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Disk full",
            Severity.High,
            FixedNow.UtcDateTime.AddMinutes(-5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WindowNotPositive_SkipsLookupAndCreates(int windowMinutes)
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);
        var service = CreateService(windowMinutes);

        var result = await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_TrimsWhitespace_AndSaves()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateAlertRequest
        {
            Title = "  Memory leak (resolved)  ",
            Description = "  Cleared after restart  ",
            Severity = Severity.Low,
            IsActive = false
        };

        var result = await _service.UpdateAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal("Memory leak (resolved)", existing.Title);
        Assert.Equal("Cleared after restart", existing.Description);
        Assert.Equal("Memory leak (resolved)", result!.Title);
        Assert.Equal("Cleared after restart", result.Description);
        Assert.Equal(Severity.Low, result.Severity);
        Assert.False(result.IsActive);
        Assert.Equal(existing.CreatedDate, result.CreatedDate); // CreatedDate must not change on update
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.UpdateAsync(5, new UpdateAlertRequest { Title = "x", Severity = Severity.Low });

        Assert.Null(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_SetsInactive_AndSaves()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.DeactivateAsync(1);

        Assert.NotNull(result);
        Assert.False(existing.IsActive);
        Assert.False(result!.IsActive);
        Assert.Equal("Memory leak", result.Title);
        Assert.Equal("Heap growing", result.Description);
        Assert.Equal(Severity.Medium, result.Severity);
        Assert.Equal(existing.CreatedDate, result.CreatedDate);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenAlreadyInactive_ReturnsCurrentResponse_AndDoesNotSave()
    {
        var existing = ExistingAlert();
        existing.IsActive = false;
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.DeactivateAsync(1);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.Equal(existing.CreatedDate, result.CreatedDate);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenMissing_ReturnsNull_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.DeactivateAsync(5);

        Assert.Null(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesAndReturnsTrue()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.DeleteAsync(7);

        Assert.False(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Alert AlertWithTags(params string[] tagNames)
    {
        var alert = ExistingAlert();
        foreach (var name in tagNames)
        {
            alert.Tags.Add(new Tag { Name = name });
        }

        return alert;
    }

    private static AddAlertTagsRequest TagsRequest(params string[] tags) => new() { Tags = tags.ToList() };

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(5, TagsRequest("disk"));

        Assert.Equal(AddTagsStatus.AlertNotFound, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_WithNewTags_NormalizesAndAddsThem()
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, TagsRequest("  Disk ", "NETWORK"));

        Assert.Equal(AddTagsStatus.Added, result.Status);
        Assert.NotNull(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(names => names.SequenceEqual(new[] { "disk", "network" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_CollapsesCaseVariants()
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        await _service.AddTagsAsync(1, TagsRequest("Disk", "disk", " DISK "));

        _repository.Verify(r => r.AddTagsAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(names => names.SequenceEqual(new[] { "disk" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_IgnoresAlreadyAssigned()
    {
        var alert = AlertWithTags("disk");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        await _service.AddTagsAsync(1, TagsRequest("DISK", "cpu"));

        _repository.Verify(r => r.AddTagsAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(names => names.SequenceEqual(new[] { "cpu" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllAlreadyAssigned_ReturnsAdded_WithoutCallingRepository()
    {
        var alert = AlertWithTags("disk", "cpu");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, TagsRequest("Disk", "CPU"));

        Assert.Equal(AddTagsStatus.Added, result.Status);
        Assert.Equal(new[] { "cpu", "disk" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WithExactlyTenTags_IsAllowed()
    {
        var alert = AlertWithTags("t1", "t2", "t3", "t4", "t5");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, TagsRequest("t6", "t7", "t8", "t9", "t10"));

        Assert.Equal(AddTagsStatus.Added, result.Status);
        _repository.Verify(r => r.AddTagsAsync(alert, It.Is<IReadOnlyCollection<string>>(n => n.Count == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenOverLimit_ReturnsTagLimitExceeded_AndAddsNothing()
    {
        var alert = AlertWithTags("t1", "t2", "t3", "t4", "t5");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, TagsRequest("t6", "t7", "t8", "t9", "t10", "t11"));

        Assert.Equal(AddTagsStatus.TagLimitExceeded, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_AlreadyAssignedTagsDoNotCountTowardLimit()
    {
        var alert = AlertWithTags("t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9", "t10");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, TagsRequest("T1", "t2"));

        Assert.Equal(AddTagsStatus.Added, result.Status);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(7, "disk");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse()
    {
        var alert = AlertWithTags("cpu");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "disk", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RemoveTagAsync(1, "disk");

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_NormalizesTag_AndReturnsTrue()
    {
        var alert = AlertWithTags("disk");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "disk", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RemoveTagAsync(1, "  DISK ");

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(alert, "disk", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_NullTag_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveTagAsync(1, null!));
    }
}
