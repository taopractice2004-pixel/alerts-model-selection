using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AlertService.API.Tests.Services;

public class AlertManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
                It.IsAny<string>(),
                It.IsAny<Severity>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            _configuration,
            NullLogger<AlertManagementService>.Instance);
    }

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
    public async Task GetTrendsAsync_ReturnsBucketsOldestFirst_WithZeroFillForMissingDaysAndSeverities()
    {
        // FixedNow is 2026-09-01T12:00:00Z, so a 3-day window covers 2026-08-30..2026-09-01.
        var request = new AlertTrendsQueryRequest { Days = 3 };
        _repository.Setup(r => r.GetTrendCountsAsync(
                new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, Severity Severity, int Count)>
            {
                (new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), Severity.High, 2),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), Severity.Critical, 1),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), Severity.Low, 3)
            });

        var result = await _service.GetTrendsAsync(request);

        Assert.Equal(3, result.Buckets.Count);
        Assert.Equal(new DateOnly(2026, 8, 30), result.Buckets[0].Date);
        Assert.Equal(new DateOnly(2026, 8, 31), result.Buckets[1].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[2].Date);

        Assert.Equal(2, result.Buckets[0].TotalCount);
        Assert.Equal(2, result.Buckets[0].SeverityCounts.High);
        Assert.Equal(0, result.Buckets[0].SeverityCounts.Low);

        Assert.Equal(0, result.Buckets[1].TotalCount);
        Assert.Equal(0, result.Buckets[1].SeverityCounts.Critical);

        Assert.Equal(4, result.Buckets[2].TotalCount);
        Assert.Equal(3, result.Buckets[2].SeverityCounts.Low);
        Assert.Equal(1, result.Buckets[2].SeverityCounts.Critical);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.Medium);
        Assert.Equal(0, result.Buckets[2].SeverityCounts.High);
    }

    [Fact]
    public async Task GetTrendsAsync_DefaultDays_QueriesSevenDayWindowEndingToday()
    {
        var request = new AlertTrendsQueryRequest();
        _repository.Setup(r => r.GetTrendCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, Severity Severity, int Count)>());

        var result = await _service.GetTrendsAsync(request);

        Assert.Equal(7, result.Buckets.Count);
        _repository.Verify(r => r.GetTrendCountsAsync(
            new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.False(result.IsDuplicate);
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
    public async Task CreateAsync_WhenRecentActiveDuplicateExists_ReturnsExistingAlert_AndDoesNotSave()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateAlertRequest { Title = "  Memory leak  ", Severity = Severity.Medium };

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsDuplicate);
        Assert.Equal(7, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredSuppressionWindow_WhenComputingCutoff()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AlertConstants.DuplicateSuppressionWindowMinutesConfigKey] = "30"
            })
            .Build();
        var service = new AlertManagementService(_repository.Object, _timeProvider.Object, configuration, NullLogger<AlertManagementService>.Instance);
        DateTime? capturedCutoff = null;
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, Severity, DateTime, CancellationToken>((_, _, cutoff, _) => capturedCutoff = cutoff)
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.Low });

        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(-30), capturedCutoff);
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
    public async Task AddTagsAsync_WhenMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(99, new AddTagsRequest { Tags = ["outage"] });

        Assert.Null(result);
        _repository.Verify(r => r.GetOrCreateTagsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_AddsNewTags_DedupesCaseInsensitive_AndSaves()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Id = 1, Name = "network" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.GetOrCreateTagsAsync(
                It.Is<IEnumerable<string>>(names => names.Count() == 1 && names.Single() == "Outage"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tag> { new() { Id = 2, Name = "Outage" } });

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = ["Outage", "NETWORK"] });

        Assert.NotNull(result);
        Assert.Equal(new[] { "network", "Outage" }, result!.Tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenExceedsMaxTags_ThrowsAndDoesNotSave()
    {
        var existing = ExistingAlert();
        for (var i = 0; i < AlertConstants.MaxTagsPerAlert; i++)
        {
            existing.Tags.Add(new Tag { Id = i + 1, Name = $"tag-{i}" });
        }

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAsync<TagLimitExceededException>(
            () => _service.AddTagsAsync(1, new AddTagsRequest { Tags = ["one-too-many"] }));

        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(99, "network");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsNull_AndDoesNotSave()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "network");

        Assert.Null(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesTag_CaseInsensitively_AndSaves()
    {
        var existing = ExistingAlert();
        existing.Tags.Add(new Tag { Id = 1, Name = "network" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "NETWORK");

        Assert.NotNull(result);
        Assert.Empty(result!.Tags);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
