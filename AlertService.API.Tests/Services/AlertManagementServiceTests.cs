using AlertService.API.Exceptions;
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
            _timeProvider.Object,
            Microsoft.Extensions.Options.Options.Create(new AlertDuplicateSuppressionOptions
            {
                DuplicateSuppressionWindowMinutes = 15
            }),
            NullLogger<AlertManagementService>.Instance);
    }

    private static Alert ExistingAlert(int id = 1, params string[] tags)
    {
        var alert = new Alert
        {
            Id = id,
            Title = "Memory leak",
            Description = "Heap growing",
            Severity = Severity.Medium,
            CreatedDate = FixedNow.UtcDateTime.AddDays(-1),
            IsActive = true
        };

        alert.AlertTags = tags
            .Select((tag, index) => new AlertTag
            {
                Alert = alert,
                AlertId = id,
                TagId = index + 1,
                Tag = new Tag
                {
                    Id = index + 1,
                    Name = tag,
                    NormalizedName = tag.ToUpperInvariant()
                }
            })
            .ToList();

        return alert;
    }

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
            Tag = "ops",
            Search = "disk",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "ops", "disk", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "ops", "disk", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsResponse()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert(1, "ops", "disk"));

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Memory leak", result!.Title);
        Assert.Equal(Severity.Medium, result.Severity);
        Assert.Equal(new[] { "disk", "ops" }, result.Tags);
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
    public async Task GetTrendsAsync_BuildsOldestFirstZeroFilledBuckets()
    {
        _repository.Setup(r => r.GetDailySeverityCountsAsync(
                new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DayUtc, Severity Severity, int Count)>
            {
                (new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), Severity.Low, 2),
                (new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), Severity.Critical, 1),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), Severity.Medium, 3)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 3 });

        Assert.Equal(3, result.Count);

        Assert.Equal(new DateOnly(2026, 8, 30), result[0].Day);
        Assert.Equal(3, result[0].TotalCount);
        Assert.Equal(2, result[0].SeverityCounts.Low);
        Assert.Equal(0, result[0].SeverityCounts.Medium);
        Assert.Equal(0, result[0].SeverityCounts.High);
        Assert.Equal(1, result[0].SeverityCounts.Critical);

        Assert.Equal(new DateOnly(2026, 8, 31), result[1].Day);
        Assert.Equal(0, result[1].TotalCount);
        Assert.Equal(0, result[1].SeverityCounts.Low);
        Assert.Equal(0, result[1].SeverityCounts.Medium);
        Assert.Equal(0, result[1].SeverityCounts.High);
        Assert.Equal(0, result[1].SeverityCounts.Critical);

        Assert.Equal(new DateOnly(2026, 9, 1), result[2].Day);
        Assert.Equal(3, result[2].TotalCount);
        Assert.Equal(0, result[2].SeverityCounts.Low);
        Assert.Equal(3, result[2].SeverityCounts.Medium);
        Assert.Equal(0, result[2].SeverityCounts.High);
        Assert.Equal(0, result[2].SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_WhenDaysOmitted_UsesDefaultSevenDayWindow()
    {
        _repository.Setup(r => r.GetDailySeverityCountsAsync(
                new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(DateTime DayUtc, Severity Severity, int Count)>());

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest());

        Assert.Equal(7, result.Count);
        Assert.Equal(new DateOnly(2026, 8, 26), result[0].Day);
        Assert.Equal(new DateOnly(2026, 9, 1), result[^1].Day);
        Assert.All(result, bucket =>
        {
            Assert.Equal(0, bucket.TotalCount);
            Assert.Equal(0, bucket.SeverityCounts.Low);
            Assert.Equal(0, bucket.SeverityCounts.Medium);
            Assert.Equal(0, bucket.SeverityCounts.High);
            Assert.Equal(0, bucket.SeverityCounts.Critical);
        });
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
                It.IsAny<string>(),
                It.IsAny<Severity>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
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
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Service down",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-15),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRecentActiveDuplicateExists_ReturnsExistingAlert_AndDoesNotSave()
    {
        var existing = ExistingAlert(12);
        existing.Title = "Service down";
        existing.Severity = Severity.Critical;
        existing.CreatedDate = FixedNow.UtcDateTime.AddMinutes(-5);

        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
            It.IsAny<string>(),
            It.IsAny<Severity>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = "  service down  ",
            Description = " Payments API ",
            Severity = Severity.Critical,
            IsActive = true
        });

        Assert.True(result.DuplicateSuppressed);
        Assert.Equal(12, result.Alert.Id);
        Assert.Equal("Service down", result.Alert.Title);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "service down",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-15),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task AddTagsAsync_TrimsAndDedupesRequestedTags_AndReturnsUpdatedResponse()
    {
        var existing = ExistingAlert(1, "ops");
        IReadOnlyCollection<string>? capturedTags = null;

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.AddTagsAsync(1, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<int, IReadOnlyCollection<string>, CancellationToken>((_, tags, _) =>
            {
                capturedTags = tags;
                existing.AlertTags.Add(new AlertTag
                {
                    Alert = existing,
                    AlertId = existing.Id,
                    TagId = 2,
                    Tag = new Tag { Id = 2, Name = "Disk", NormalizedName = "DISK" }
                });
                existing.AlertTags.Add(new AlertTag
                {
                    Alert = existing,
                    AlertId = existing.Id,
                    TagId = 3,
                    Tag = new Tag { Id = 3, Name = "cpu", NormalizedName = "CPU" }
                });
            })
            .ReturnsAsync(existing);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest
        {
            Tags = new[] { " ops ", "Disk", "disk", " cpu ", "CPU" }
        });

        Assert.NotNull(result);
        Assert.Equal(new[] { "ops", "Disk", "cpu" }, capturedTags);
        Assert.Equal(new[] { "cpu", "Disk", "ops" }, result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenMergedTagCountExceedsLimit_ThrowsValidationException()
    {
        var existing = ExistingAlert(1, "one", "two", "three", "four", "five", "six", "seven", "eight", "nine");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new[] { "ten", "eleven" } }));

        Assert.Contains(nameof(AddAlertTagsRequest.Tags), exception.Errors.Keys);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull_AndDoesNotPersist()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(42, new AddAlertTagsRequest { Tags = new[] { "ops" } });

        Assert.Null(result);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task RemoveTagAsync_WhenTagIsBlank_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.RemoveTagAsync(1, "   "));

        Assert.Contains("tag", exception.Errors.Keys);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsNull()
    {
        var existing = ExistingAlert(1, "ops");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.RemoveTagAsync(1, "DISK", It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(1, " disk ");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_ReturnsUpdatedResponse()
    {
        var existing = ExistingAlert(1, "ops", "disk");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.RemoveTagAsync(1, "DISK", It.IsAny<CancellationToken>()))
            .Callback<int, string, CancellationToken>((_, _, _) =>
            {
                var diskTag = existing.AlertTags.Single(tag => tag.Tag.NormalizedName == "DISK");
                existing.AlertTags.Remove(diskTag);
            })
            .ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, " disk ");

        Assert.NotNull(result);
        Assert.Equal(new[] { "ops" }, result!.Tags);
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
}
