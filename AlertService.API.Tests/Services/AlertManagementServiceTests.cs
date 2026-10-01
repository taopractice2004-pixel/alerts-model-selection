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
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = CreateService();
    }

    private AlertManagementService CreateService(string? duplicateSuppressionWindowMinutes = "15")
    {
        return new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            CreateConfiguration(duplicateSuppressionWindowMinutes),
            NullLogger<AlertManagementService>.Instance);
    }

    private static IConfiguration CreateConfiguration(string? duplicateSuppressionWindowMinutes)
    {
        var settings = new Dictionary<string, string?>();

        if (duplicateSuppressionWindowMinutes is not null)
        {
            settings["Alerts:DuplicateSuppressionWindowMinutes"] = duplicateSuppressionWindowMinutes;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static Alert ExistingAlert(int id = 1) => new()
    {
        Id = id,
        Title = "Memory leak",
        Description = "Heap growing",
        Severity = Severity.Medium,
        CreatedDate = FixedNow.UtcDateTime.AddDays(-1),
        IsActive = true,
        AlertTags =
        [
            new AlertTag
            {
                AlertId = id,
                TagId = 1,
                Tag = new Tag
                {
                    Id = 1,
                    Name = "ops"
                }
            }
        ]
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
    public async Task GetTrendsAsync_ReturnsOldestFirstBuckets_WithMissingDaysAndSeveritiesZeroFilled()
    {
        _repository.Setup(r => r.GetDailyTrendsAsync(
                new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AlertTrendCount(
                    new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                    1,
                    1,
                    0,
                    0,
                    0),
                new AlertTrendCount(
                    new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    3,
                    0,
                    0,
                    2,
                    1)
            ]);

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 4 });

        Assert.Equal(4, result.Days);
        Assert.Equal(4, result.Buckets.Count);
        Assert.Equal(
            new[]
            {
                new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            result.Buckets.Select(bucket => bucket.Date).ToArray());
        Assert.Equal(new[] { 1, 0, 0, 3 }, result.Buckets.Select(bucket => bucket.TotalCount).ToArray());

        var missingBucket = result.Buckets[1];
        Assert.Equal(0, missingBucket.SeverityCounts.Low);
        Assert.Equal(0, missingBucket.SeverityCounts.Medium);
        Assert.Equal(0, missingBucket.SeverityCounts.High);
        Assert.Equal(0, missingBucket.SeverityCounts.Critical);

        var populatedBucket = result.Buckets[3];
        Assert.Equal(0, populatedBucket.SeverityCounts.Low);
        Assert.Equal(0, populatedBucket.SeverityCounts.Medium);
        Assert.Equal(2, populatedBucket.SeverityCounts.High);
        Assert.Equal(1, populatedBucket.SeverityCounts.Critical);
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
        Assert.False(result.WasDuplicateSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        Assert.Empty(result.Alert.Tags);
        _repository.Verify(r => r.GetActiveDuplicateAsync("Service down", Severity.Critical, FixedNow.UtcDateTime.AddMinutes(-15), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateExists_ReturnsExistingAlert_AndDoesNotSave()
    {
        var existing = ExistingAlert(11);
        _repository.Setup(r => r.GetActiveDuplicateAsync("Service down", Severity.Critical, FixedNow.UtcDateTime.AddMinutes(-15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest
        {
            Title = "  Service down  ",
            Description = " Payments API ",
            Severity = Severity.Critical,
            IsActive = true
        });

        Assert.True(result.WasDuplicateSuppressed);
        Assert.Equal(existing.Id, result.Alert.Id);
        Assert.Equal(existing.Title, result.Alert.Title);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuppressionWindowIsDisabled_SavesNewAlert()
    {
        var service = CreateService("0");
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 12)
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await service.CreateAsync(new CreateAlertRequest
        {
            Title = "  Service down  ",
            Severity = Severity.Critical,
            IsActive = true
        });

        Assert.False(result.WasDuplicateSuppressed);
        Assert.Equal(12, result.Alert.Id);
        _repository.Verify(r => r.GetActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(5, new AddAlertTagsRequest { Tags = ["ops"] });

        Assert.Equal(AlertTagOperationStatus.NotFound, result.Status);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_DeduplicatesCaseInsensitiveTags_AndPersists()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((existingAlert, tags, _) =>
            {
                foreach (var tag in tags.Where(tag => !existingAlert.AlertTags.Any(alertTag => string.Equals(alertTag.Tag.Name, tag, StringComparison.OrdinalIgnoreCase))))
                {
                    var nextId = existingAlert.AlertTags.Count + 1;
                    existingAlert.AlertTags.Add(new AlertTag
                    {
                        AlertId = existingAlert.Id,
                        TagId = nextId,
                        Tag = new Tag
                        {
                            Id = nextId,
                            Name = tag
                        }
                    });
                }
            })
            .Returns(Task.CompletedTask);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = [" Ops ", "database", "DATABASE"] });

        Assert.Equal(AlertTagOperationStatus.Success, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Equal(["database", "ops"], result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(tags => tags.Count == 2 && tags.Contains("Ops") && tags.Contains("database")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenCombinedTagCountExceedsLimit_ReturnsValidationFailed()
    {
        var alert = new Alert
        {
            Id = 1,
            Title = "Memory leak",
            Severity = Severity.Medium,
            CreatedDate = FixedNow.UtcDateTime,
            IsActive = true,
            AlertTags = Enumerable.Range(1, AlertConstants.MaxTagsPerAlert)
                .Select(index => new AlertTag
                {
                    AlertId = 1,
                    TagId = index,
                    Tag = new Tag
                    {
                        Id = index,
                        Name = $"tag-{index}"
                    }
                })
                .ToList()
        };

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = ["extra"] });

        Assert.Equal(AlertTagOperationStatus.ValidationFailed, result.Status);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsNotFound()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, "database");

        Assert.Equal(AlertTagOperationStatus.NotFound, result.Status);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_RemovesAndReturnsUpdatedAlert()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "ops", It.IsAny<CancellationToken>()))
            .Callback<Alert, string, CancellationToken>((existingAlert, tag, _) =>
            {
                var alertTag = existingAlert.AlertTags.Single(alertTag => string.Equals(alertTag.Tag.Name, tag, StringComparison.OrdinalIgnoreCase));
                existingAlert.AlertTags.Remove(alertTag);
            })
            .Returns(Task.CompletedTask);

        var result = await _service.RemoveTagAsync(1, "ops");

        Assert.Equal(AlertTagOperationStatus.Success, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Empty(result.Alert!.Tags);
        _repository.Verify(r => r.RemoveTagAsync(alert, "ops", It.IsAny<CancellationToken>()), Times.Once);
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
}
