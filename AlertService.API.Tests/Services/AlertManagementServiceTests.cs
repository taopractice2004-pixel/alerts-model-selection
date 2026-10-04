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

    private static Alert ExistingAlertWithTags(int id = 1, params string[] tags)
    {
        var alert = ExistingAlert(id);
        foreach (var tag in tags)
        {
            alert.AlertTags.Add(new AlertTag
            {
                AlertId = id,
                Alert = alert,
                Tag = new Tag { Name = tag }
            });
        }

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
    public async Task GetAllAsync_PassesTagFilterToRepository()
    {
        var request = new AlertQueryRequest
        {
            Tag = "infra",
            Search = "disk",
            SortBy = "title",
            SortDirection = "asc",
            Page = 1,
            PageSize = 5
        };
        _repository
            .Setup(r => r.GetAllAsync(null, null, null, null, "disk", "title", "asc", 1, 5, "infra", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlertWithTags(1, "infra") }, 1));

        _ = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(null, null, null, null, "disk", "title", "asc", 1, 5, "infra", It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetByIdAsync_WhenExists_MapsTagsAlphabetically()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingAlertWithTags(1, "ops", "infra"));

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(new[] { "infra", "ops" }, result!.Tags);
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
    public async Task GetDailyTrendsAsync_UsesUtcDateRangeAndReturnsOldestFirstBuckets()
    {
        _repository
            .Setup(r => r.GetDailyTrendCountsAsync(
                new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DayUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>
            {
                (new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), 2, 1, 0, 1, 0),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 1, 0, 0, 0, 1)
            });

        var result = await _service.GetDailyTrendsAsync(4);

        Assert.Equal(4, result.Count);
        Assert.Equal(new[]
        {
            new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        }, result.Select(item => item.DayUtc).ToArray());

        Assert.Equal(0, result[0].TotalCount);
        Assert.Equal(0, result[0].SeverityCounts.Low);
        Assert.Equal(0, result[0].SeverityCounts.Medium);
        Assert.Equal(0, result[0].SeverityCounts.High);
        Assert.Equal(0, result[0].SeverityCounts.Critical);

        Assert.Equal(2, result[1].TotalCount);
        Assert.Equal(1, result[1].SeverityCounts.Low);
        Assert.Equal(0, result[1].SeverityCounts.Medium);
        Assert.Equal(1, result[1].SeverityCounts.High);
        Assert.Equal(0, result[1].SeverityCounts.Critical);

        Assert.Equal(0, result[2].TotalCount);
        Assert.Equal(1, result[3].TotalCount);
        Assert.Equal(0, result[3].SeverityCounts.Low);
        Assert.Equal(0, result[3].SeverityCounts.Medium);
        Assert.Equal(0, result[3].SeverityCounts.High);
        Assert.Equal(1, result[3].SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetDailyTrendsAsync_UsesTodayBasedInclusiveExclusiveBounds()
    {
        _repository
            .Setup(r => r.GetDailyTrendCountsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DayUtc, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>());

        _ = await _service.GetDailyTrendsAsync(7);

        _repository.Verify(r => r.GetDailyTrendCountsAsync(
            new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository
            .Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
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
        Assert.Equal(10, result.Id);
        Assert.Equal(Severity.Critical, result.Severity);
        _repository.Verify(
            r => r.FindActiveDuplicateAsync(
                "Service down",
                Severity.Critical,
                FixedNow.UtcDateTime.AddMinutes(-AlertSuppressionOptions.DefaultDuplicateSuppressionWindowMinutes),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWithSuppressionAsync_WhenDuplicateExists_ReturnsSuppressedWithoutAdding()
    {
        var duplicate = ExistingAlert(77);
        _repository
            .Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var request = new CreateAlertRequest
        {
            Title = "  Memory leak  ",
            Description = "Heap growing",
            Severity = Severity.Medium,
            IsActive = true
        };

        var result = await _service.CreateWithSuppressionAsync(request);

        Assert.True(result.DuplicateSuppressed);
        Assert.Equal(77, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateWithSuppressionAsync_UsesConfiguredSuppressionWindowMinutes()
    {
        _repository
            .Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert alert, CancellationToken _) =>
            {
                alert.Id = 99;
                return alert;
            });

        var service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            NullLogger<AlertManagementService>.Instance,
            Options.Create(new AlertSuppressionOptions { DuplicateSuppressionWindowMinutes = 30 }));

        var request = new CreateAlertRequest
        {
            Title = "  Memory leak  ",
            Severity = Severity.Medium
        };

        var result = await service.CreateWithSuppressionAsync(request);

        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(
            r => r.FindActiveDuplicateAsync(
                "Memory leak",
                Severity.Medium,
                FixedNow.UtcDateTime.AddMinutes(-30),
                It.IsAny<CancellationToken>()),
            Times.Once);
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
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(42, new AddTagsRequest { Tags = ["infra"] });

        Assert.Equal(TagAssignmentStatus.AlertNotFound, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenMaxTagsWouldBeExceeded_ReturnsMaxTagsExceeded()
    {
        var existingTags = Enumerable.Range(1, 9).Select(i => $"tag-{i}").ToArray();
        var alert = ExistingAlertWithTags(1, existingTags);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = ["new-1", "new-2"] });

        Assert.Equal(TagAssignmentStatus.MaxTagsExceeded, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesAndTrimsCaseInsensitivelyBeforeRepositoryCall()
    {
        var alert = ExistingAlertWithTags(1, "ops");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(1, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingAlertWithTags(1, "ops", "infra"));

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = ["  infra  ", "INFRA", "ops"] });

        Assert.Equal(TagAssignmentStatus.Success, result.Status);
        _repository.Verify(
            r => r.AddTagsAsync(
                1,
                It.Is<IReadOnlyCollection<string>>(tags => tags.Count == 1 && tags.Single() == "infra"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllTagsAlreadyAssigned_DoesNotPersistAndReturnsCurrentAlert()
    {
        var alert = ExistingAlertWithTags(1, "infra", "ops");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = ["INFRA", " ops "] });

        Assert.Equal(TagAssignmentStatus.Success, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Equal(new[] { "infra", "ops" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WithWhitespaceOnlyTag_ReturnsFalseWithoutRepositoryCall()
    {
        var result = await _service.RemoveTagAsync(1, "   ");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenRepositoryRemovesTag_ReturnsTrueAndTrimsTag()
    {
        _repository.Setup(r => r.RemoveTagAsync(1, "infra", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RemoveTagAsync(1, "  infra  ");

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(1, "infra", It.IsAny<CancellationToken>()), Times.Once);
    }
}
