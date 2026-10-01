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

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        var duplicateOptions = Options.Create(new DuplicateSuppressionOptions { WindowMinutes = 15 });
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            duplicateOptions,
            NullLogger<AlertManagementService>.Instance);
    }

    private static Alert ExistingAlert(int id = 1) => new()
    {
        Id = id,
        Title = "Memory leak",
        Description = "Heap growing",
        Severity = Severity.Medium,
        CreatedDate = FixedNow.UtcDateTime.AddDays(-1),
        IsActive = true,
        Tags = []
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
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", null, "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", null, "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task GetTrendsAsync_ReturnsContiguousOldestFirstBuckets_WithZeroFilledDaysAndSeverities()
    {
        var request = new AlertTrendsQueryRequest { Days = 4 };
        var day0 = FixedNow.UtcDateTime.Date.AddDays(-3);
        var day1 = FixedNow.UtcDateTime.Date.AddDays(-2);
        var day3 = FixedNow.UtcDateTime.Date;

        _repository.Setup(r => r.GetDailyTrendsAsync(day0, FixedNow.UtcDateTime.Date.AddDays(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DayUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>
            {
                (day0, 2, 0, 1, 1, 0),
                (day1, 1, 1, 0, 0, 0),
                (day3, 3, 0, 0, 1, 2)
            });

        var result = await _service.GetTrendsAsync(request);

        Assert.Equal(4, result.Days);
        Assert.Equal(4, result.Buckets.Count);
        Assert.Equal(new[] { day0, day1, day1.AddDays(1), day3 }, result.Buckets.Select(b => b.Date).ToArray());
        Assert.Equal(0, result.Buckets[2].TotalCount);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.Low);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.Medium);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.High);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.Critical);
        Assert.Equal(2, result.Buckets[3].SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_DefaultDays_UsesSevenDayWindowIncludingToday()
    {
        var request = new AlertTrendsQueryRequest();
        var expectedStart = FixedNow.UtcDateTime.Date.AddDays(-6);
        var expectedEndExclusive = FixedNow.UtcDateTime.Date.AddDays(1);
        _repository.Setup(r => r.GetDailyTrendsAsync(expectedStart, expectedEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DayUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>());

        var result = await _service.GetTrendsAsync(request);

        _repository.Verify(r => r.GetDailyTrendsAsync(expectedStart, expectedEndExclusive, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(AlertConstants.DefaultTrendDays, result.Buckets.Count);
        Assert.Equal(expectedStart, result.Buckets[0].Date);
        Assert.Equal(FixedNow.UtcDateTime.Date, result.Buckets[^1].Date);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => { saved = a; a.Id = 10; })
            .ReturnsAsync((Alert a, CancellationToken _) => a);
        _repository.Setup(r => r.GetLatestActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

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
        Assert.False(result.IsDuplicateSuppressed);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateExistsWithinWindow_ReturnsSuppressedResult_AndSkipsCreate()
    {
        var existing = ExistingAlert(9);
        existing.Title = "Service down";
        existing.Severity = Severity.Critical;
        _repository.Setup(r => r.GetLatestActiveDuplicateAsync(
                " Service down ",
                Severity.Critical,
                FixedNow.UtcDateTime.AddMinutes(-15),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = " Service down ",
            Severity = Severity.Critical
        });

        Assert.True(result.IsDuplicateSuppressed);
        Assert.Equal(9, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task AddTagAsync_WhenMissing_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagAsync(3, "Ops");

        Assert.Equal(AlertTagOperationStatus.AlertNotFound, result.Status);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagAsync_WhenDuplicateCaseInsensitive_ReturnsAlreadyAssigned()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Name = "Ops", NormalizedName = "OPS" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.AddTagAsync(1, "ops");

        Assert.Equal(AlertTagOperationStatus.TagAlreadyAssigned, result.Status);
        _repository.Verify(r => r.GetOrCreateTagAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagAsync_WhenMaxTagsReached_ReturnsMaxTagsReached()
    {
        var existing = ExistingAlert();
        for (var i = 0; i < 10; i++)
        {
            existing.Tags.Add(new Tag { Name = $"tag-{i}", NormalizedName = $"TAG-{i}" });
        }

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.AddTagAsync(1, "Ops");

        Assert.Equal(AlertTagOperationStatus.MaxTagsReached, result.Status);
        _repository.Verify(r => r.GetOrCreateTagAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagAsync_WhenValid_AddsTagAndSaves()
    {
        var existing = ExistingAlert();
        var tag = new Tag { Name = "Ops", NormalizedName = "OPS" };
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.GetOrCreateTagAsync("Ops", "OPS", It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var result = await _service.AddTagAsync(1, " Ops ");

        Assert.Equal(AlertTagOperationStatus.Success, result.Status);
        Assert.Contains(existing.Tags, t => t.NormalizedName == "OPS");
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagMissingOnAlert_ReturnsNotAssigned()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Name = "Ops", NormalizedName = "OPS" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "SRE");

        Assert.Equal(AlertTagOperationStatus.TagNotAssigned, result.Status);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssigned_RemovesTagAndSaves()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Name = "Ops", NormalizedName = "OPS" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "ops");

        Assert.Equal(AlertTagOperationStatus.Success, result.Status);
        Assert.DoesNotContain(existing.Tags, t => t.NormalizedName == "OPS");
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
