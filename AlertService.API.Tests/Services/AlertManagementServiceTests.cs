using AlertService.API.Configuration;
using AlertService.API.Services;
using AlertService.Common.Constants;
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
        Options.Create(new AlertSuppressionOptions { WindowMinutes = windowMinutes }),
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
            Tag = "database",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "database", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "database", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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

    // --- GetTrendsAsync (ALERT-412) ---

    private void SetupDailyCounts(params (DateTime DayUtc, Severity Severity, int Count)[] rows) =>
        _repository.Setup(r => r.GetDailySeverityCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToList());

    [Fact]
    public async Task GetTrendsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetTrendsAsync(null!));
    }

    // AC1 + AC2: the default request returns 7 buckets, one per UTC day, oldest first, ending on today's UTC day.
    [Fact]
    public async Task GetTrendsAsync_DefaultRequest_Returns7UtcDayBuckets_OldestFirst()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest());

        Assert.Equal(7, result.Days);
        Assert.Equal(7, result.Buckets.Count);
        var expectedDays = Enumerable.Range(0, 7)
            .Select(offset => new DateOnly(2026, 8, 26).AddDays(offset))
            .ToArray();
        Assert.Equal(expectedDays, result.Buckets.Select(b => b.Date).ToArray());
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    // AC1 + AC2: an explicit days value returns exactly that many contiguous oldest-first buckets.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task GetTrendsAsync_WithExplicitDays_ReturnsThatManyContiguousBuckets(int days)
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = days });

        Assert.Equal(days, result.Days);
        Assert.Equal(days, result.Buckets.Count);
        for (var i = 1; i < result.Buckets.Count; i++)
        {
            Assert.Equal(result.Buckets[i - 1].Date.AddDays(1), result.Buckets[i].Date);
        }
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    // AC2 boundary: days=1 yields a single bucket for today's UTC day.
    [Fact]
    public async Task GetTrendsAsync_WithDaysOne_ReturnsSingleBucketForToday()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(new DateOnly(2026, 9, 1), bucket.Date);
    }

    // AC2: the service queries the repository with a half-open [startDay 00:00, today+1 00:00) UTC range.
    [Fact]
    public async Task GetTrendsAsync_QueriesRepositoryWithHalfOpenUtcRange()
    {
        DateTime capturedFrom = default;
        DateTime capturedTo = default;
        _repository.Setup(r => r.GetDailySeverityCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((from, to, _) => { capturedFrom = from; capturedTo = to; })
            .ReturnsAsync(new List<(DateTime DayUtc, Severity Severity, int Count)>());

        await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 7 });

        Assert.Equal(new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc), capturedFrom);
        Assert.Equal(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), capturedTo);
    }

    // AC3 + AC5: each bucket carries its total count and per-severity counts (Low..Critical) for its day.
    [Fact]
    public async Task GetTrendsAsync_MapsPerSeverityAndTotalCountsPerDay()
    {
        var day1 = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyCounts(
            (day1, Severity.Low, 2),
            (day1, Severity.Critical, 1),
            (day2, Severity.Medium, 3),
            (day2, Severity.High, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 2 });

        var first = result.Buckets[0];
        Assert.Equal(new DateOnly(2026, 8, 31), first.Date);
        Assert.Equal(2, first.SeverityCounts.Low);
        Assert.Equal(0, first.SeverityCounts.Medium);
        Assert.Equal(0, first.SeverityCounts.High);
        Assert.Equal(1, first.SeverityCounts.Critical);
        Assert.Equal(3, first.TotalCount);

        var second = result.Buckets[1];
        Assert.Equal(new DateOnly(2026, 9, 1), second.Date);
        Assert.Equal(0, second.SeverityCounts.Low);
        Assert.Equal(3, second.SeverityCounts.Medium);
        Assert.Equal(4, second.SeverityCounts.High);
        Assert.Equal(0, second.SeverityCounts.Critical);
        Assert.Equal(7, second.TotalCount);
    }

    // AC4: days with no alerts, and severities with no alerts within a day, appear with explicit zero counts.
    [Fact]
    public async Task GetTrendsAsync_ZeroFillsDaysAndSeveritiesWithNoAlerts()
    {
        var onlyDay = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupDailyCounts((onlyDay, Severity.High, 5));

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 3 });

        Assert.Equal(3, result.Buckets.Count);
        foreach (var empty in result.Buckets.Take(2))
        {
            Assert.Equal(0, empty.TotalCount);
            Assert.Equal(0, empty.SeverityCounts.Low);
            Assert.Equal(0, empty.SeverityCounts.Medium);
            Assert.Equal(0, empty.SeverityCounts.High);
            Assert.Equal(0, empty.SeverityCounts.Critical);
        }

        var populated = result.Buckets[^1];
        Assert.Equal(new DateOnly(2026, 9, 1), populated.Date);
        Assert.Equal(5, populated.SeverityCounts.High);
        Assert.Equal(0, populated.SeverityCounts.Low);
        Assert.Equal(0, populated.SeverityCounts.Medium);
        Assert.Equal(0, populated.SeverityCounts.Critical);
        Assert.Equal(5, populated.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
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

        var (status, alert) = await _service.CreateAsync(request);

        Assert.Equal(AlertCreationStatus.Created, status);
        Assert.NotNull(saved);
        Assert.Equal("Service down", saved!.Title);
        Assert.Equal("Payments API", saved.Description);
        Assert.Equal(FixedNow.UtcDateTime, saved.CreatedDate);
        Assert.Equal(10, alert.Id);
        Assert.Equal(Severity.Critical, alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    // AC1 + AC2: an active same-title/same-severity alert within the window suppresses creation.
    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateWithinWindow_SuppressesAndReturnsExisting()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindActiveDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateAlertRequest
        {
            Title = "  Memory leak  ",
            Description = "Heap growing",
            Severity = Severity.Medium,
            IsActive = true
        };

        var (status, alert) = await _service.CreateAsync(request);

        Assert.Equal(AlertCreationStatus.DuplicateSuppressed, status);
        Assert.Equal(7, alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC3: a non-duplicate alert is created and reported as Created.
    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_ReturnsCreatedStatus()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 3)
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var (status, alert) = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = "New alert",
            Severity = Severity.High,
            IsActive = true
        });

        Assert.Equal(AlertCreationStatus.Created, status);
        Assert.Equal(3, alert.Id);
    }

    // AC4: the duplicate lookup uses the configured window (now - WindowMinutes) as the threshold.
    [Fact]
    public async Task CreateAsync_UsesConfiguredWindowMinutes_ForDuplicateLookup()
    {
        const int configuredWindow = 30;
        var service = CreateService(configuredWindow);
        DateTime capturedThreshold = default;
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, Severity, DateTime, CancellationToken>((_, _, threshold, _) => capturedThreshold = threshold)
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await service.CreateAsync(new CreateAlertRequest { Title = "Any", Severity = Severity.Low, IsActive = true });

        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(-configuredWindow), capturedThreshold);
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

    [Fact]
    public async Task AddTagsAsync_WhenAlertExists_AddsTags_AndReturnsResponseWithTags()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.AddTagsToAlertAsync(existing, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((a, names, _) =>
            {
                foreach (var n in names) a.Tags.Add(new Tag { Name = n });
            })
            .Returns(Task.CompletedTask);

        var (status, alert) = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "database", "prod" } });

        Assert.Equal(TagOperationStatus.Success, status);
        Assert.NotNull(alert);
        Assert.Equal(new[] { "database", "prod" }, alert!.Tags);
        _repository.Verify(r => r.AddTagsToAlertAsync(existing,
            It.Is<IReadOnlyCollection<string>>(t => t.Count == 2 && t.Contains("database") && t.Contains("prod")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_TrimsAndDedupesRequestTagsCaseInsensitively()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        IReadOnlyCollection<string>? captured = null;
        _repository.Setup(r => r.AddTagsToAlertAsync(existing, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((_, names, _) => captured = names)
            .Returns(Task.CompletedTask);

        var (status, _) = await _service.AddTagsAsync(1, new AddTagsRequest
        {
            Tags = new List<string> { " Database ", "database", "DATABASE", "  ", "prod" }
        });

        Assert.Equal(TagOperationStatus.Success, status);
        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Count);
        Assert.Contains(captured, t => string.Equals(t, "Database", StringComparison.Ordinal));
        Assert.Contains("prod", captured);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagAlreadyPresent_IsNoOp()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Name = "database" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var (status, alert) = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "DATABASE" } });

        Assert.Equal(TagOperationStatus.Success, status);
        Assert.NotNull(alert);
        Assert.Equal(new[] { "database" }, alert!.Tags);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultingSetExceedsMax_ReturnsTagLimitExceeded_AndDoesNotPersist()
    {
        var existing = ExistingAlert();
        for (var i = 0; i < AlertConstants.MaxTagsPerAlert; i++)
        {
            existing.Tags.Add(new Tag { Name = $"tag{i}" });
        }
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var (status, alert) = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "overflow" } });

        Assert.Equal(TagOperationStatus.TagLimitExceeded, status);
        Assert.Null(alert);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var (status, alert) = await _service.AddTagsAsync(42, new AddTagsRequest { Tags = new List<string> { "database" } });

        Assert.Equal(TagOperationStatus.AlertNotFound, status);
        Assert.Null(alert);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_RemovesTag_AndReturnsSuccess()
    {
        var existing = ExistingAlert();
        var tag = new Tag { Name = "database" };
        existing.Tags.Add(tag);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var status = await _service.RemoveTagAsync(1, "DATABASE");

        Assert.Equal(TagOperationStatus.Success, status);
        _repository.Verify(r => r.RemoveTagFromAlertAsync(existing, tag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var status = await _service.RemoveTagAsync(5, "database");

        Assert.Equal(TagOperationStatus.AlertNotFound, status);
        _repository.Verify(r => r.RemoveTagFromAlertAsync(It.IsAny<Alert>(), It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsTagNotAssigned()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Name = "prod" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var status = await _service.RemoveTagAsync(1, "database");

        Assert.Equal(TagOperationStatus.TagNotAssigned, status);
        _repository.Verify(r => r.RemoveTagFromAlertAsync(It.IsAny<Alert>(), It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
