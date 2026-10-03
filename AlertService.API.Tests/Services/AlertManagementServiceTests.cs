using AlertService.API.Services;
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
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            CreateConfiguration(),
            NullLogger<AlertManagementService>.Instance);
    }

    private static IConfiguration CreateConfiguration(string? suppressionWindowMinutes = "15")
    {
        var values = new Dictionary<string, string?>();
        if (suppressionWindowMinutes is not null)
        {
            values["Alerts:DuplicateSuppressionWindowMinutes"] = suppressionWindowMinutes;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
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
    public async Task AddTagsAsync_WhenRequestIsNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull_AndDoesNotAddTags()
    {
        _repository.Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(77, new AddAlertTagsRequest { Tags = ["ops"] });

        Assert.Null(result);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_DeduplicatesCaseInsensitiveAndTrims_AndReturnsMappedTags()
    {
        var existing = ExistingAlert(1);
        existing.Tags.Add(new Tag { Value = "Ops", NormalizedValue = "OPS" });

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.AddTagsAsync(1, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Alert
            {
                Id = 1,
                Title = existing.Title,
                Description = existing.Description,
                Severity = existing.Severity,
                CreatedDate = existing.CreatedDate,
                IsActive = existing.IsActive,
                Tags =
                [
                    new Tag { Value = "database", NormalizedValue = "DATABASE" },
                    new Tag { Value = "Ops", NormalizedValue = "OPS" }
                ]
            });

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = [" ops ", "Database", "database"] });

        Assert.NotNull(result);
        Assert.Equal(new[] { "database", "Ops" }, result!.Tags.ToArray());
        _repository.Verify(r => r.AddTagsAsync(
            1,
            It.Is<IReadOnlyList<string>>(tags => tags.Count == 2 && tags.Contains("ops") && tags.Contains("Database")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenMoreThan10TotalTags_ThrowsAndDoesNotWrite()
    {
        var existing = ExistingAlert(1);
        for (var index = 0; index < 10; index++)
        {
            existing.Tags.Add(new Tag { Value = $"tag-{index}", NormalizedValue = $"TAG-{index}" });
        }

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = ["new-tag"] }));

        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WithWhitespaceTag_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemoveTagAsync(1, "   "));
    }

    [Fact]
    public async Task RemoveTagAsync_WithLongTag_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemoveTagAsync(1, new string('a', 31)));
    }

    [Fact]
    public async Task RemoveTagAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        _repository.Setup(r => r.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RemoveTagAsync(1, "ops");

        Assert.False(result);
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
    public async Task GetDailyTrendsAsync_ReturnsLastNDaysOldestFirst_WithZeroFilledBuckets()
    {
        _repository.Setup(r => r.GetDailySeverityCountsAsync(
                new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DateUtc, int LowCount, int MediumCount, int HighCount, int CriticalCount)>
            {
                (new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), 1, 0, 2, 0),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 0, 1, 0, 1)
            });

        var result = await _service.GetDailyTrendsAsync(3);

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2026, 8, 30), result[0].Date);
        Assert.Equal(3, result[0].TotalCount);
        Assert.Equal(1, result[0].SeverityCounts.Low);
        Assert.Equal(2, result[0].SeverityCounts.High);

        Assert.Equal(new DateOnly(2026, 8, 31), result[1].Date);
        Assert.Equal(0, result[1].TotalCount);
        Assert.Equal(0, result[1].SeverityCounts.Low);
        Assert.Equal(0, result[1].SeverityCounts.Medium);
        Assert.Equal(0, result[1].SeverityCounts.High);
        Assert.Equal(0, result[1].SeverityCounts.Critical);

        Assert.Equal(new DateOnly(2026, 9, 1), result[2].Date);
        Assert.Equal(2, result[2].TotalCount);
        Assert.Equal(1, result[2].SeverityCounts.Medium);
        Assert.Equal(1, result[2].SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetDailyTrendsAsync_MapsSeverityCounts_UsingSummaryOrdering()
    {
        _repository.Setup(r => r.GetDailySeverityCountsAsync(
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime DateUtc, int LowCount, int MediumCount, int HighCount, int CriticalCount)>
            {
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 2, 3, 4, 5)
            });

        var result = await _service.GetDailyTrendsAsync(1);

        Assert.Single(result);
        Assert.Equal(14, result[0].TotalCount);
        Assert.Equal(2, result[0].SeverityCounts.Low);
        Assert.Equal(3, result[0].SeverityCounts.Medium);
        Assert.Equal(4, result[0].SeverityCounts.High);
        Assert.Equal(5, result[0].SeverityCounts.Critical);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
                It.IsAny<string>(),
                It.IsAny<Severity>(),
                It.IsAny<DateTime>(),
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
        Assert.False(result.IsDuplicateSuppressed);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateFound_ReturnsSuppressedResult_AndDoesNotAdd()
    {
        var duplicate = ExistingAlert(12);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
                It.IsAny<string>(),
                Severity.Critical,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var request = new CreateAlertRequest
        {
            Title = "  Memory leak ",
            Description = "ignored",
            Severity = Severity.Critical,
            IsActive = true
        };

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsDuplicateSuppressed);
        Assert.Equal(duplicate.Id, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredSuppressionWindow()
    {
        var service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            CreateConfiguration("42"),
            NullLogger<AlertManagementService>.Instance);

        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(
                It.IsAny<string>(),
                It.IsAny<Severity>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) =>
            {
                a.Id = 55;
                return a;
            });

        var request = new CreateAlertRequest { Title = "Disk full", Severity = Severity.High };

        _ = await service.CreateAsync(request);

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Disk full",
            Severity.High,
            FixedNow.UtcDateTime.AddMinutes(-42),
            FixedNow.UtcDateTime,
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
}
