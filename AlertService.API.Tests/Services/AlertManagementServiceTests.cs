using System.ComponentModel.DataAnnotations;
using AlertService.API.Options;
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
        _service = new AlertManagementService(
            _repository.Object,
            Microsoft.Extensions.Options.Options.Create(new AlertSuppressionOptions { DuplicateWindowMinutes = 5 }),
            _timeProvider.Object,
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
        Tags = [new Tag { Id = 1, Name = "ops", NormalizedName = "OPS" }]
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
            Tag = "ops",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "ops", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "ops", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Equal(["ops"], result.Tags);
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
    public async Task GetTrendsAsync_MapsRepositoryBucketsWithZeroFilledSeverityCounts()
    {
        _repository.Setup(r => r.GetDailyTrendsAsync(new DateOnly(2026, 8, 30), new DateOnly(2026, 9, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                (new DateOnly(2026, 8, 30), 0, 0, 0, 0, 0),
                (new DateOnly(2026, 8, 31), 2, 1, 0, 1, 0),
                (new DateOnly(2026, 9, 1), 1, 0, 0, 0, 1)
            ]);

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 3 });

        Assert.Equal(3, result.Days);
        Assert.Equal(3, result.Buckets.Count);
        Assert.Equal(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), result.Buckets[0].DateUtc);
        Assert.Equal(0, result.Buckets[0].TotalCount);
        Assert.Equal(0, result.Buckets[0].SeverityCounts.Low);
        Assert.Equal(1, result.Buckets[1].SeverityCounts.Low);
        Assert.Equal(1, result.Buckets[1].SeverityCounts.High);
        Assert.Equal(1, result.Buckets[2].SeverityCounts.Critical);
        _repository.Verify(r => r.GetDailyTrendsAsync(new DateOnly(2026, 8, 30), new DateOnly(2026, 9, 1), It.IsAny<CancellationToken>()), Times.Once);
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
        _repository.Setup(r => r.FindActiveDuplicateAsync("  Service down  ", Severity.Critical, FixedNow.UtcDateTime.AddMinutes(-5), It.IsAny<CancellationToken>()))
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

        var result = await _service.CreateAsync(request);

        Assert.NotNull(saved);
        Assert.Equal("Service down", saved!.Title);
        Assert.Equal("Payments API", saved.Description);
        Assert.Equal(FixedNow.UtcDateTime, saved.CreatedDate);
        Assert.False(result.IsDuplicateSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        Assert.Empty(result.Alert.Tags);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WhenRecentActiveDuplicateExists_ReturnsSuppressedAlert_WithoutSaving()
    {
        var existing = ExistingAlert(7);
        existing.Title = "Service down";
        existing.Severity = Severity.Critical;
        existing.CreatedDate = FixedNow.UtcDateTime.AddMinutes(-2);
        _repository.Setup(r => r.FindActiveDuplicateAsync(" Service down ", Severity.Critical, FixedNow.UtcDateTime.AddMinutes(-5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = " Service down ",
            Description = "Payments API",
            Severity = Severity.Critical,
            IsActive = true
        });

        Assert.True(result.IsDuplicateSuppressed);
        Assert.Equal(7, result.Alert.Id);
        Assert.Equal("Service down", result.Alert.Title);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenMatchingSeverityDiffers_CreatesNewAlert()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync("Disk full", Severity.High, FixedNow.UtcDateTime.AddMinutes(-5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((alert, _) => alert.Id = 21)
            .ReturnsAsync((Alert alert, CancellationToken _) => alert);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = "Disk full",
            Severity = Severity.High,
            IsActive = true
        });

        Assert.False(result.IsDuplicateSuppressed);
        Assert.Equal(21, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenInactiveOrExpiredMatchesExist_CreatesNewAlert()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync("Disk full", Severity.High, FixedNow.UtcDateTime.AddMinutes(-5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((alert, _) => alert.Id = 22)
            .ReturnsAsync((Alert alert, CancellationToken _) => alert);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = "Disk full",
            Severity = Severity.High,
            IsActive = true
        });

        Assert.False(result.IsDuplicateSuppressed);
        Assert.Equal(22, result.Alert.Id);
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
        Assert.Equal(["ops"], result.Tags);
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
        Assert.Equal(["ops"], result.Tags);
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
    public async Task AddTagsAsync_DeduplicatesCaseInsensitively_AndAssignsOnlyNewTags()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new AssignAlertTagsRequest
        {
            Tags = [" ops ", "DB", "db", "api"]
        };

        var result = await _service.AddTagsAsync(1, request);

        Assert.NotNull(result);
        _repository.Verify(r => r.AssignTagsAsync(existing, It.Is<IReadOnlyList<string>>(tags => tags.SequenceEqual(new[] { "DB", "api" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(55, new AssignAlertTagsRequest { Tags = ["ops"] });

        Assert.Null(result);
        _repository.Verify(r => r.AssignTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenMaximumWouldBeExceeded_ThrowsValidationException()
    {
        var existing = ExistingAlert();
        existing.Tags = Enumerable.Range(1, 10)
            .Select(index => new Tag { Id = index, Name = $"tag-{index}", NormalizedName = $"TAG-{index}" })
            .ToList();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, new AssignAlertTagsRequest { Tags = ["extra"] }));

        _repository.Verify(r => r.AssignTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_RemovesTagAndSaves()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, " ops ");

        Assert.True(result);
        Assert.Empty(existing.Tags);
        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsFalse()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "db");

        Assert.False(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
