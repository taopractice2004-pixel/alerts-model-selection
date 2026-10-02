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
using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Tests.Services;

public class AlertManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private const int SuppressionWindowMinutes = 15;

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            NullLogger<AlertManagementService>.Instance,
            Options.Create(new AlertSuppressionOptions { WindowMinutes = SuppressionWindowMinutes }));
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

    [Fact]
    public async Task GetTrendsAsync_WithoutDays_Returns7BucketsOldestFirst()
    {
        _repository.Setup(r => r.GetDailyCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(DateTime, Severity, int)>());

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest());

        Assert.Equal(7, result.Days);
        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(FixedNow.UtcDateTime.Date.AddDays(-6), result.Buckets[0].Date);
        Assert.Equal(FixedNow.UtcDateTime.Date, result.Buckets[^1].Date);
        for (var i = 1; i < result.Buckets.Count; i++)
        {
            Assert.Equal(result.Buckets[i - 1].Date.AddDays(1), result.Buckets[i].Date);
        }
    }

    [Fact]
    public async Task GetTrendsAsync_QueriesRepositoryWithUtcDayWindow()
    {
        DateTime? from = null;
        DateTime? toExclusive = null;
        _repository.Setup(r => r.GetDailyCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((f, t, _) => { from = f; toExclusive = t; })
            .ReturnsAsync(Array.Empty<(DateTime, Severity, int)>());

        await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        Assert.Equal(FixedNow.UtcDateTime.Date.AddDays(-2), from);
        Assert.Equal(FixedNow.UtcDateTime.Date.AddDays(1), toExclusive);
    }

    [Fact]
    public async Task GetTrendsAsync_FillsMissingDaysAndSeveritiesWithZero()
    {
        var today = FixedNow.UtcDateTime.Date;
        _repository.Setup(r => r.GetDailyCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>
            {
                (today, Severity.High, 2),
                (today, Severity.Low, 1)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 2 });

        var yesterday = result.Buckets[0];
        Assert.Equal(today.AddDays(-1), yesterday.Date);
        Assert.Equal(0, yesterday.TotalCount);
        Assert.Equal(0, yesterday.SeverityCounts.Low);
        Assert.Equal(0, yesterday.SeverityCounts.Medium);
        Assert.Equal(0, yesterday.SeverityCounts.High);
        Assert.Equal(0, yesterday.SeverityCounts.Critical);

        var todayBucket = result.Buckets[1];
        Assert.Equal(today, todayBucket.Date);
        Assert.Equal(1, todayBucket.SeverityCounts.Low);
        Assert.Equal(0, todayBucket.SeverityCounts.Medium);
        Assert.Equal(2, todayBucket.SeverityCounts.High);
        Assert.Equal(0, todayBucket.SeverityCounts.Critical);
        Assert.Equal(3, todayBucket.TotalCount);
    }

    [Fact]
    public async Task GetTrendsAsync_MapsAllFourSeveritiesAndTotalsTheirCounts()
    {
        var today = FixedNow.UtcDateTime.Date;
        _repository.Setup(r => r.GetDailyCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime, Severity, int)>
            {
                (today, Severity.Low, 1),
                (today, Severity.Medium, 2),
                (today, Severity.High, 3),
                (today, Severity.Critical, 4)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 1 });

        var bucket = Assert.Single(result.Buckets);
        Assert.Equal(today, bucket.Date);
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
        Assert.False(result.WasSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateWithinWindow_SuppressesAndReturnsExisting()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var request = new CreateAlertRequest { Title = existing.Title, Severity = existing.Severity };

        var result = await _service.CreateAsync(request);

        Assert.True(result.WasSuppressed);
        Assert.Equal(7, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_CreatesNewAlert()
    {
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 11)
            .ReturnsAsync((Alert a, CancellationToken _) => a);
        var request = new CreateAlertRequest { Title = "New alert", Severity = Severity.Low };

        var result = await _service.CreateAsync(request);

        Assert.False(result.WasSuppressed);
        Assert.Equal(11, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PassesRequestSeverityAndConfiguredWindowToDuplicateLookup()
    {
        string? capturedTitle = null;
        Severity? capturedSeverity = null;
        DateTime? capturedThreshold = null;
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, Severity, DateTime, CancellationToken>((title, severity, threshold, _) =>
            {
                capturedTitle = title;
                capturedSeverity = severity;
                capturedThreshold = threshold;
            })
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);
        var request = new CreateAlertRequest { Title = "Disk full", Severity = Severity.High };

        await _service.CreateAsync(request);

        Assert.Equal("Disk full", capturedTitle);
        Assert.Equal(Severity.High, capturedSeverity);
        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(-SuppressionWindowMinutes), capturedThreshold);
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

    [Fact]
    public async Task AddTagsAsync_WhenAlertExists_AddsTags_AndReturnsUpdatedAlert()
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tag>());

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "network", "disk" } });

        Assert.NotNull(result);
        Assert.Equal(new[] { "disk", "network" }, result!.Tags);
        _repository.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_DeduplicatesWithinRequest_CaseInsensitive_KeepsFirstSeenCasing()
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tag>());

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "Urgent", "urgent", "URGENT" } });

        Assert.NotNull(result);
        Assert.Equal(new[] { "Urgent" }, result!.Tags);
        Assert.Single(alert.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_IgnoresTagsAlreadyOnAlert_CaseInsensitive()
    {
        var alert = AlertWithTags("urgent");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tag>());

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "URGENT", "network" } });

        Assert.NotNull(result);
        Assert.Equal(new[] { "network", "urgent" }, result!.Tags);
        Assert.Equal(2, alert.Tags.Count);
        _repository.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllRequestedTagsAlreadyPresent_DoesNotSave()
    {
        var alert = AlertWithTags("network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "NETWORK" } });

        Assert.NotNull(result);
        Assert.Equal(new[] { "network" }, result!.Tags);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagExceedsMaxLength_ThrowsValidationException_AndDoesNotSave()
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var request = new AddTagsRequest { Tags = new List<string> { new string('x', AlertConstants.TagMaxLength + 1) } };

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, request));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddTagsAsync_WhenTagIsEmptyOrWhitespace_ThrowsValidationException(string tag)
    {
        var alert = AlertWithTags();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var request = new AddTagsRequest { Tags = new List<string> { tag } };

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, request));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultingTagCountExceedsMax_ThrowsValidationException_AndDoesNotSave()
    {
        var existing = Enumerable.Range(1, AlertConstants.MaxTagsPerAlert - 2).Select(i => $"tag{i}").ToArray();
        var alert = AlertWithTags(existing);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tag>());

        var request = new AddTagsRequest { Tags = new List<string> { "extra1", "extra2", "extra3" } };

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, request));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(42, new AddTagsRequest { Tags = new List<string> { "network" } });

        Assert.Null(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_ReusesExistingTagRow_InsteadOfCreatingNew()
    {
        var alert = AlertWithTags();
        var existingTag = new Tag { Id = 5, Name = "network" };
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.GetTagsByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingTag });

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new List<string> { "network" } });

        Assert.NotNull(result);
        Assert.Contains(alert.Tags, t => ReferenceEquals(t, existingTag));
        Assert.Equal(5, alert.Tags.Single().Id);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagAssigned_RemovesTag_ReturnsTrue()
    {
        var alert = AlertWithTags("network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "network");

        Assert.True(result);
        Assert.Empty(alert.Tags);
        _repository.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_MatchesTagCaseInsensitively()
    {
        var alert = AlertWithTags("Network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "network");

        Assert.True(result);
        Assert.Empty(alert.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(42, "network");

        Assert.False(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse_AndDoesNotSave()
    {
        var alert = AlertWithTags("network");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "disk");

        Assert.False(result);
        Assert.Single(alert.Tags);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
