using AlertService.API.Configuration;
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
            Options.Create(new DuplicateSuppressionOptions()),
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

    private static Alert AlertWithTags(int id, params string[] tagNames)
    {
        var alert = ExistingAlert(id);
        alert.AlertTags = tagNames
            .Select(name => new AlertTag { AlertId = id, Tag = new Tag { Name = name } })
            .ToList();
        return alert;
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(9, new AddTagsRequest { Tags = { "db" } });

        Assert.Equal(AddTagsStatus.AlertNotFound, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesAgainstExistingAndWithinRequest_PersistsOnlyNewNames()
    {
        var alert = AlertWithTags(1, "prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((a, names, _) =>
            {
                foreach (var name in names)
                {
                    a.AlertTags.Add(new AlertTag { Alert = a, Tag = new Tag { Name = name } });
                }
            })
            .Returns(Task.CompletedTask);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "prod", "PROD", "db", "  db  " } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        _repository.Verify(r => r.AddTagsAsync(alert, It.Is<IReadOnlyCollection<string>>(c => c.Count == 1 && c.Contains("db")), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { "db", "prod" }, result.Alert!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllTagsAlreadyPresent_ReturnsSuccessWithoutPersisting()
    {
        var alert = AlertWithTags(1, "prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "PROD" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        Assert.Equal(new[] { "prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenWouldExceedMax_ReturnsTagLimitExceeded_AndDoesNotPersist()
    {
        var existing = Enumerable.Range(1, 9).Select(i => $"tag{i}").ToArray();
        var alert = AlertWithTags(1, existing);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "new1", "new2" } });

        Assert.Equal(AddTagsStatus.TagLimitExceeded, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenReachingExactlyMax_Persists()
    {
        var existing = Enumerable.Range(1, 9).Select(i => $"tag{i}").ToArray();
        var alert = AlertWithTags(1, existing);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "tag10" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        _repository.Verify(r => r.AddTagsAsync(alert, It.Is<IReadOnlyCollection<string>>(c => c.Count == 1 && c.Contains("tag10")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(9, "db");

        Assert.Equal(RemoveTagStatus.AlertNotFound, result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagPresent_ReturnsRemoved()
    {
        var alert = AlertWithTags(1, "prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "prod", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RemoveTagAsync(1, "  prod  ");

        Assert.Equal(RemoveTagStatus.Removed, result);
        _repository.Verify(r => r.RemoveTagAsync(alert, "prod", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagAbsent_ReturnsTagNotFound()
    {
        var alert = AlertWithTags(1, "prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "db", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RemoveTagAsync(1, "db");

        Assert.Equal(RemoveTagStatus.TagNotFound, result);
    }

    [Fact]
    public async Task GetTrendsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetTrendsAsync(null!));
    }

    [Fact]
    public async Task GetTrendsAsync_DefaultDays_ReturnsSevenOldestFirstBuckets_EndingToday()
    {
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest());

        Assert.Equal(7, result.Days.Count);
        // FixedNow = 2026-09-01, so oldest = 2026-08-26 and newest = 2026-09-01.
        Assert.Equal(new DateOnly(2026, 8, 26), result.Days[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Days[^1].Date);
        Assert.True(result.Days.SequenceEqual(result.Days.OrderBy(d => d.Date)));
    }

    [Fact]
    public async Task GetTrendsAsync_QueriesHalfOpenUtcRangeCoveringRequestedDays()
    {
        DateTime? capturedFrom = null;
        DateTime? capturedTo = null;
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((from, to, _) => { capturedFrom = from; capturedTo = to; })
            .ReturnsAsync(new List<(DateTime, Severity, int)>());

        await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 3 });

        Assert.Equal(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), capturedFrom);
        Assert.Equal(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), capturedTo);
    }

    [Fact]
    public async Task GetTrendsAsync_WithSingleDay_ReturnsOneBucketForToday()
    {
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 1 });

        Assert.Single(result.Days);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Days[0].Date);
    }

    [Fact]
    public async Task GetTrendsAsync_WithMaxDays_ReturnsNinetyBuckets()
    {
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest { Days = 90 });

        Assert.Equal(90, result.Days.Count);
    }

    [Fact]
    public async Task GetTrendsAsync_ZeroFillsMissingDaysAndSeverities()
    {
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest());

        Assert.All(result.Days, day =>
        {
            Assert.Equal(0, day.TotalCount);
            Assert.Equal(0, day.SeverityCounts.Low);
            Assert.Equal(0, day.SeverityCounts.Medium);
            Assert.Equal(0, day.SeverityCounts.High);
            Assert.Equal(0, day.SeverityCounts.Critical);
        });
    }

    [Fact]
    public async Task GetTrendsAsync_MapsPerDaySeverityCountsAndTotals()
    {
        var day = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc);
        _repository.Setup(r => r.GetDailyCountsBySeverityAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>
            {
                (day, Severity.Low, 2),
                (day, Severity.High, 3),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), Severity.Critical, 1)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendQueryRequest());

        var mapped = result.Days.Single(d => d.Date == new DateOnly(2026, 8, 30));
        Assert.Equal(2, mapped.SeverityCounts.Low);
        Assert.Equal(0, mapped.SeverityCounts.Medium);
        Assert.Equal(3, mapped.SeverityCounts.High);
        Assert.Equal(0, mapped.SeverityCounts.Critical);
        Assert.Equal(5, mapped.TotalCount);

        var latest = result.Days.Single(d => d.Date == new DateOnly(2026, 9, 1));
        Assert.Equal(1, latest.SeverityCounts.Critical);
        Assert.Equal(1, latest.TotalCount);
    }
}
