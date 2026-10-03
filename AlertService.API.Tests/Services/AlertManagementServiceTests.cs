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

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = CreateService(15);
    }

    private AlertManagementService CreateService(int windowMinutes) => new(
        _repository.Object,
        _timeProvider.Object,
        Options.Create(new DuplicateSuppressionOptions { WindowMinutes = windowMinutes }),
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
                "createdDate",
                "desc",
                1,
                20,
                null,
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
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "title", "asc", 2, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "title", "asc", 2, 10, null, It.IsAny<CancellationToken>()), Times.Once);
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

    private void SetupDailyCounts(params (DateTime Date, Severity Severity, int Count)[] counts) =>
        _repository.Setup(r => r.GetDailyCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);

    [Fact]
    public async Task GetTrendsAsync_WithDefaultDays_ReturnsSevenBuckets()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest());

        Assert.Equal(7, result.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task GetTrendsAsync_ReturnsExactlyRequestedNumberOfBuckets(int days)
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = days });

        Assert.Equal(days, result.Count);
    }

    [Fact]
    public async Task GetTrendsAsync_ReturnsConsecutiveUtcDaysOldestFirst_EndingToday()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        Assert.Equal(
            new[] { new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1) },
            result.Select(b => b.Date));
    }

    [Fact]
    public async Task GetTrendsAsync_CallsRepositoryWithWindowFromUtcMidnightToTomorrowMidnight()
    {
        SetupDailyCounts();

        await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 7 });

        _repository.Verify(r => r.GetDailyCountsAsync(
            new DateTime(2026, 8, 26, 0, 0, 0),
            new DateTime(2026, 9, 2, 0, 0, 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrendsAsync_WhenNoAlerts_ReturnsZeroFilledBuckets()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 5 });

        Assert.All(result, b =>
        {
            Assert.Equal(0, b.TotalCount);
            Assert.Equal(0, b.SeverityCounts.Low);
            Assert.Equal(0, b.SeverityCounts.Medium);
            Assert.Equal(0, b.SeverityCounts.High);
            Assert.Equal(0, b.SeverityCounts.Critical);
        });
    }

    [Fact]
    public async Task GetTrendsAsync_MapsCountsToCorrectDayAndSeverity_AndZeroFillsTheRest()
    {
        SetupDailyCounts(
            (new DateTime(2026, 8, 30), Severity.Low, 2),
            (new DateTime(2026, 8, 30), Severity.Critical, 5),
            (new DateTime(2026, 9, 1), Severity.Medium, 3),
            (new DateTime(2026, 9, 1), Severity.High, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        var first = result[0];
        Assert.Equal(new DateOnly(2026, 8, 30), first.Date);
        Assert.Equal(2, first.SeverityCounts.Low);
        Assert.Equal(0, first.SeverityCounts.Medium);
        Assert.Equal(0, first.SeverityCounts.High);
        Assert.Equal(5, first.SeverityCounts.Critical);

        var middle = result[1];
        Assert.Equal(new DateOnly(2026, 8, 31), middle.Date);
        Assert.Equal(0, middle.TotalCount);
        Assert.Equal(0, middle.SeverityCounts.Low);
        Assert.Equal(0, middle.SeverityCounts.Medium);
        Assert.Equal(0, middle.SeverityCounts.High);
        Assert.Equal(0, middle.SeverityCounts.Critical);

        var last = result[2];
        Assert.Equal(new DateOnly(2026, 9, 1), last.Date);
        Assert.Equal(0, last.SeverityCounts.Low);
        Assert.Equal(3, last.SeverityCounts.Medium);
        Assert.Equal(4, last.SeverityCounts.High);
        Assert.Equal(0, last.SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_TotalCountEqualsSumOfSeverityCounts()
    {
        SetupDailyCounts(
            (new DateTime(2026, 9, 1), Severity.Low, 1),
            (new DateTime(2026, 9, 1), Severity.Medium, 2),
            (new DateTime(2026, 9, 1), Severity.High, 3),
            (new DateTime(2026, 9, 1), Severity.Critical, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 1 });

        var bucket = Assert.Single(result);
        Assert.Equal(10, bucket.TotalCount);
        Assert.Equal(
            bucket.SeverityCounts.Low + bucket.SeverityCounts.Medium + bucket.SeverityCounts.High + bucket.SeverityCounts.Critical,
            bucket.TotalCount);
    }

    [Fact]
    public async Task GetTrendsAsync_WithNullRequest_ThrowsArgumentNullException()
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
        Assert.False(result.IsDuplicateSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_DuplicateFound_DoesNotAdd_AndReturnsExistingAlert()
    {
        var existing = AlertWithTags(7, "urgent");
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "MEMORY LEAK", Severity = Severity.Medium });

        Assert.True(result.IsDuplicateSuppressed);
        Assert.Equal(7, result.Alert.Id);
        Assert.Equal("Memory leak", result.Alert.Title);
        Assert.Equal(new[] { "urgent" }, result.Alert.Tags);
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

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Brand new", Severity = Severity.Low });

        Assert.False(result.IsDuplicateSuppressed);
        Assert.Equal(11, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_QueriesRepositoryWithTrimmedTitle_Severity_AndWindowStart()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await _service.CreateAsync(new CreateAlertRequest { Title = "  Service down  ", Severity = Severity.Critical });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Service down",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredWindow()
    {
        var service = CreateService(60);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Disk full",
            Severity.High,
            FixedNow.UtcDateTime.AddMinutes(-60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WhenWindowNotPositive_SkipsDuplicateCheck_AndCreates(int windowMinutes)
    {
        var service = CreateService(windowMinutes);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        Assert.False(result.IsDuplicateSuppressed);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
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

    private static Alert AlertWithTags(int id, params string[] tagNames)
    {
        var alert = ExistingAlert(id);
        foreach (var name in tagNames)
        {
            alert.Tags.Add(new Tag { Name = name });
        }

        return alert;
    }

    private void SetupExistingTagRows(params Tag[] rows) =>
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    [Fact]
    public async Task GetAllAsync_ForwardsTagFilterToRepository()
    {
        var request = new AlertQueryRequest { Tag = "Urgent" };
        _repository.Setup(r => r.GetAllAsync(null, null, null, null, null, "createdDate", "desc", 1, 20, "Urgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { AlertWithTags(1, "urgent") }, 1));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(null, null, null, null, null, "createdDate", "desc", 1, 20, "Urgent", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(result.Items);
        Assert.Equal(new[] { "urgent" }, result.Items[0].Tags);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTagsSortedAscending()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags(1, "zeta", "alpha", "mid"));

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(new[] { "alpha", "mid", "zeta" }, result!.Tags);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAlertHasNoTags_ReturnsEmptyTagList()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result!.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmptyTagList()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "New", Severity = Severity.Low });

        Assert.Empty(result.Alert.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_NewTags_AreTrimmedLowercased_CreatedAndSavedOnce()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "  Urgent ", "NETWORK" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Equal(new[] { "network", "urgent" }, result.Alert!.Tags);
        Assert.Equal(new[] { "network", "urgent" }, existing.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_DuplicatesWithinRequest_AreStoredOnce_CaseInsensitively()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        IReadOnlyCollection<string>? lookedUp = null;
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, CancellationToken>((names, _) => lookedUp = names)
            .ReturnsAsync(Array.Empty<Tag>());

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "Urgent", "urgent", " URGENT " } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Equal(new[] { "urgent" }, result.Alert!.Tags);
        Assert.Single(existing.Tags);
        Assert.Equal(new[] { "urgent" }, lookedUp);
    }

    [Fact]
    public async Task AddTagsAsync_TagAlreadyOnAlert_DifferentCase_IsNotDuplicated()
    {
        var existing = AlertWithTags(1, "urgent");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "URGENT", "network" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Equal(new[] { "network", "urgent" }, result.Alert!.Tags);
        Assert.Equal(2, existing.Tags.Count);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_OnlyTagsAlreadyOnAlert_ReturnsAdded_AndDoesNotSave()
    {
        var existing = AlertWithTags(1, "urgent", "network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "Urgent", " NETWORK" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Equal(new[] { "network", "urgent" }, result.Alert!.Tags);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.GetTagsByNamesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRows_AndCreatesMissingOnes()
    {
        var existing = ExistingAlert();
        var storedTag = new Tag { Id = 7, Name = "urgent" };
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows(storedTag);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "Urgent", "fresh" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Contains(storedTag, existing.Tags);
        var created = Assert.Single(existing.Tags, t => t.Name == "fresh");
        Assert.Equal(0, created.Id);
        Assert.Equal(2, existing.Tags.Count);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_ReachingExactlyTenTags_IsAllowed()
    {
        var existing = AlertWithTags(1, "t1", "t2", "t3", "t4", "t5", "t6", "t7");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "t8", "t9", "t10" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Equal(10, result.Alert!.Tags.Count);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_ExceedingTenTags_ReturnsTagLimitExceeded_AndChangesNothing()
    {
        var existing = AlertWithTags(1, "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "t9", "t10", "t11" } });

        Assert.Equal(AddAlertTagsStatus.TagLimitExceeded, result.Status);
        Assert.Null(result.Alert);
        Assert.Equal(8, existing.Tags.Count);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_LimitCountsOnlyDistinctNewTags()
    {
        var existing = AlertWithTags(1, "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingTagRows();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new List<string> { "T9", "t10", "T10" } });

        Assert.Equal(AddAlertTagsStatus.Added, result.Status);
        Assert.Equal(10, existing.Tags.Count);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(5, new AddAlertTagsRequest { Tags = new List<string> { "urgent" } });

        Assert.Equal(AddAlertTagsStatus.AlertNotFound, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Theory]
    [InlineData("urgent")]
    [InlineData("URGENT")]
    [InlineData("  Urgent  ")]
    public async Task RemoveTagAsync_WhenAssigned_RemovesAssignmentCaseInsensitively_AndSaves(string tag)
    {
        var existing = AlertWithTags(1, "urgent", "network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, tag);

        Assert.True(result);
        Assert.Equal(new[] { "network" }, existing.Tags.Select(t => t.Name));
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(5, "urgent");

        Assert.False(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse_AndDoesNotSave()
    {
        var existing = AlertWithTags(1, "network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "urgent");

        Assert.False(result);
        Assert.Single(existing.Tags);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_NullTag_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveTagAsync(1, null!));
    }
}
