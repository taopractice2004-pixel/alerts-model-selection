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
        NullLogger<AlertManagementService>.Instance,
        Options.Create(new AlertSuppressionOptions { WindowMinutes = windowMinutes }));

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
            Tag = "network",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "network", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "network", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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

    // AC1: no days supplied -> default of 7 produces 7 daily buckets ending at today (UTC).
    [Fact]
    public async Task GetTrendsAsync_WithDefaultDays_ReturnsSevenBuckets()
    {
        _repository.Setup(r => r.GetDailyTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, int, int, int, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendsRequest());

        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(new DateOnly(2026, 8, 26), result.Buckets[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    // AC2: exactly N buckets, one per UTC calendar day, ordered oldest first, over the expected UTC range.
    [Fact]
    public async Task GetTrendsAsync_ReturnsBucketsOldestFirstForEachUtcDay()
    {
        DateTime capturedFrom = default;
        DateTime capturedTo = default;
        _repository.Setup(r => r.GetDailyTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((from, to, _) => { capturedFrom = from; capturedTo = to; })
            .ReturnsAsync(new List<(DateTime, int, int, int, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendsRequest { Days = 3 });

        Assert.Equal(
            new[] { new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1) },
            result.Buckets.Select(b => b.Date).ToArray());
        Assert.Equal(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), capturedFrom);
        Assert.Equal(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), capturedTo);
    }

    // AC3: days and severities absent from the repository result are zero-filled; TotalCount is the sum of severity counts.
    [Fact]
    public async Task GetTrendsAsync_ZeroFillsMissingDaysAndSeverities()
    {
        _repository.Setup(r => r.GetDailyTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, int, int, int, int)>
            {
                (new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc), 2, 0, 1, 0)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendsRequest { Days = 3 });

        var byDate = result.Buckets.ToDictionary(b => b.Date);

        var empty1 = byDate[new DateOnly(2026, 8, 30)];
        Assert.Equal(0, empty1.TotalCount);
        Assert.Equal(0, empty1.SeverityCounts.Low);
        Assert.Equal(0, empty1.SeverityCounts.Critical);

        var populated = byDate[new DateOnly(2026, 8, 31)];
        Assert.Equal(3, populated.TotalCount);
        Assert.Equal(2, populated.SeverityCounts.Low);
        Assert.Equal(0, populated.SeverityCounts.Medium);
        Assert.Equal(1, populated.SeverityCounts.High);
        Assert.Equal(0, populated.SeverityCounts.Critical);

        var empty2 = byDate[new DateOnly(2026, 9, 1)];
        Assert.Equal(0, empty2.TotalCount);
    }

    // AC4: severity counts map onto Low/Medium/High/Critical in the same order as the summary endpoint; TotalCount is their sum.
    [Fact]
    public async Task GetTrendsAsync_MapsSeverityCountsInSummaryOrder()
    {
        _repository.Setup(r => r.GetDailyTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, int, int, int, int)>
            {
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 1, 2, 3, 4)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendsRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(new DateOnly(2026, 9, 1), bucket.Date);
        Assert.Equal(1, bucket.SeverityCounts.Low);
        Assert.Equal(2, bucket.SeverityCounts.Medium);
        Assert.Equal(3, bucket.SeverityCounts.High);
        Assert.Equal(4, bucket.SeverityCounts.Critical);
        Assert.Equal(10, bucket.TotalCount);
    }

    [Fact]
    public async Task GetTrendsAsync_NullRequest_Throws()
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
        Assert.False(result.Suppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC1: no duplicate -> inserts and returns a non-suppressed Created result.
    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_ReturnsCreated_AndInserts()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 7)
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Service down", Severity = Severity.High });

        Assert.False(result.Suppressed);
        Assert.Equal(7, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC1: an active duplicate within the window -> suppressed result, existing alert returned, no insert.
    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateWithinWindow_Suppresses_AndDoesNotInsert()
    {
        var existing = ExistingAlert(99);
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Medium });

        Assert.True(result.Suppressed);
        Assert.Equal(99, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC3: the window start passed to the repository is computed from the configured WindowMinutes.
    [Fact]
    public async Task CreateAsync_UsesConfiguredWindowMinutes_ForWindowStart()
    {
        const int configuredWindow = 42;
        var service = CreateService(configuredWindow);
        DateTime capturedWindowStart = default;
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, Severity, DateTime, CancellationToken>((_, _, windowStart, _) => capturedWindowStart = windowStart)
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await service.CreateAsync(new CreateAlertRequest { Title = "Service down", Severity = Severity.High });

        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(-configuredWindow), capturedWindowStart);
    }

    // AC1/AC4: the request title (trimmed) and severity are passed to the duplicate lookup.
    [Fact]
    public async Task CreateAsync_PassesTitleAndSeverity_ToDuplicateLookup()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await _service.CreateAsync(new CreateAlertRequest { Title = "  Service down  ", Severity = Severity.Critical });

        _repository.Verify(r => r.FindActiveDuplicateAsync(
            "  Service down  ",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-WindowMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound_AndDoesNotPersist()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(42, new AddTagsRequest { Tags = new() { "db" } });

        Assert.Equal(AddTagsOutcome.AlertNotFound, result.Outcome);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenValid_AddsTrimmedTags_AndReturnsSuccessWithMergedOrderedTags()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        SetupAddTagsCallback();

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new() { "  database  ", "cache" } });

        Assert.Equal(AddTagsOutcome.Success, result.Outcome);
        Assert.NotNull(result.Alert);
        Assert.Equal(new[] { "cache", "database" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsToAlertAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "database", "cache" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesAgainstRequestAndExistingTags_CaseInsensitively()
    {
        var alert = ExistingAlert();
        alert.Tags.Add(new Tag { Name = "database" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        SetupAddTagsCallback();

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new() { "DATABASE", "cache", "Cache", " cache " } });

        Assert.Equal(AddTagsOutcome.Success, result.Outcome);
        _repository.Verify(r => r.AddTagsToAlertAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "cache" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllTagsAlreadyPresent_ReturnsSuccess_AndDoesNotPersist()
    {
        var alert = ExistingAlert();
        alert.Tags.Add(new Tag { Name = "database" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new() { "DATABASE" } });

        Assert.Equal(AddTagsOutcome.Success, result.Outcome);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultingTotalExceedsLimit_ReturnsTagLimitExceeded_AndPersistsNothing()
    {
        var alert = ExistingAlert();
        for (var i = 0; i < AlertConstants.MaxTagsPerAlert; i++)
        {
            alert.Tags.Add(new Tag { Name = $"tag{i}" });
        }
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new() { "overflow" } });

        Assert.Equal(AddTagsOutcome.TagLimitExceeded, result.Outcome);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenDuplicateKeepsTotalAtLimit_ReturnsSuccess()
    {
        var alert = ExistingAlert();
        for (var i = 0; i < AlertConstants.MaxTagsPerAlert; i++)
        {
            alert.Tags.Add(new Tag { Name = $"tag{i}" });
        }
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new() { "TAG0" } });

        Assert.Equal(AddTagsOutcome.Success, result.Outcome);
        _repository.Verify(r => r.AddTagsToAlertAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_RemovesAssignment_CaseInsensitive_AndReturnsTrue()
    {
        var alert = ExistingAlert();
        var tag = new Tag { Name = "database" };
        alert.Tags.Add(tag);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "DATABASE");

        Assert.True(result);
        Assert.DoesNotContain(tag, alert.Tags);
        _repository.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse_AndDoesNotSave()
    {
        var alert = ExistingAlert();
        alert.Tags.Add(new Tag { Name = "database" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "cache");

        Assert.False(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(5, "database");

        Assert.False(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupAddTagsCallback() =>
        _repository.Setup(r => r.AddTagsToAlertAsync(
                It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((a, names, _) =>
            {
                foreach (var name in names)
                {
                    a.Tags.Add(new Tag { Name = name });
                }
            })
            .Returns(Task.CompletedTask);
}
