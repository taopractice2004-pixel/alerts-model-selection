using System.ComponentModel.DataAnnotations;
using AlertService.API.Services;
using AlertService.API.Options;
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
    private const int DefaultSuppressionWindowMinutes = 15;

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = CreateService();
    }

    private AlertManagementService CreateService(int suppressionWindowMinutes = DefaultSuppressionWindowMinutes)
    {
        return new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            Microsoft.Extensions.Options.Options.Create(new DuplicateAlertOptions
            {
                DuplicateSuppressionWindowMinutes = suppressionWindowMinutes
            }),
            NullLogger<AlertManagementService>.Instance);
    }

    private static Alert ExistingAlert(
        int id = 1,
        string title = "Memory leak",
        Severity severity = Severity.Medium,
        DateTime? createdDate = null,
        bool isActive = true) => new()
    {
        Id = id,
        Title = title,
        Description = "Heap growing",
        Severity = severity,
        CreatedDate = createdDate ?? FixedNow.UtcDateTime.AddDays(-1),
        IsActive = isActive
    };

    private static Alert ExistingAlertWithTags(int id = 1, params string[] tags)
    {
        var alert = ExistingAlert(id);

        foreach (var tag in tags)
        {
            alert.Tags.Add(new Tag { Name = tag });
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
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", request.Tag, "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", request.Tag, "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetTrendsAsync_FillsMissingDaysAndMapsSeverityCounts()
    {
        var request = new AlertTrendQueryRequest { Days = 7 };

        _repository.Setup(r => r.GetTrendCountsAsync(
                new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, int TotalCount, int LowCount, int MediumCount, int HighCount, int CriticalCount)>
            {
                (new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc), 2, 1, 1, 0, 0),
                (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 3, 0, 0, 1, 2)
            });

        var result = await _service.GetTrendsAsync(request);

        Assert.Equal(7, result.Count);
        Assert.Equal(new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc), result[0].Date);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), result[^1].Date);

        Assert.Equal(0, result[0].TotalCount);
        Assert.Equal(2, result[1].TotalCount);
        Assert.Equal(1, result[1].SeverityCounts.Low);
        Assert.Equal(1, result[1].SeverityCounts.Medium);
        Assert.Equal(0, result[1].SeverityCounts.High);
        Assert.Equal(0, result[1].SeverityCounts.Critical);

        Assert.Equal(0, result[2].TotalCount);
        Assert.Equal(3, result[^1].TotalCount);
        Assert.Equal(0, result[^1].SeverityCounts.Low);
        Assert.Equal(0, result[^1].SeverityCounts.Medium);
        Assert.Equal(1, result[^1].SeverityCounts.High);
        Assert.Equal(2, result[^1].SeverityCounts.Critical);
        Assert.All(result, bucket => Assert.Equal(DateTimeKind.Utc, bucket.Date.Kind));

        _repository.Verify(r => r.GetTrendCountsAsync(
            new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDate_TrimsInput_AndSaves()
    {
        Alert? saved = null;
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
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
        Assert.False(result.DuplicateSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.FindActiveDuplicateAsync(
            "Service down",
            Severity.Critical,
            FixedNow.UtcDateTime.AddMinutes(-DefaultSuppressionWindowMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenQualifyingDuplicateExists_ReturnsExistingAlert_AndDoesNotSave()
    {
        var request = new CreateAlertRequest
        {
            Title = "  MEMORY leak  ",
            Severity = Severity.Medium,
            Description = "ignored"
        };
        var duplicate = ExistingAlert(
            id: 7,
            title: "Memory leak",
            severity: Severity.Medium,
            createdDate: FixedNow.UtcDateTime.AddMinutes(-5),
            isActive: true);

        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var result = await _service.CreateAsync(request);

        Assert.True(result.DuplicateSuppressed);
        Assert.Equal(7, result.Alert.Id);
        Assert.Equal("Memory leak", result.Alert.Title);
        _repository.Verify(r => r.FindActiveDuplicateAsync(
            "MEMORY leak",
            Severity.Medium,
            FixedNow.UtcDateTime.AddMinutes(-DefaultSuppressionWindowMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredSuppressionWindowForDuplicateLookup()
    {
        var service = CreateService(30);
        var request = new CreateAlertRequest
        {
            Title = " Service down ",
            Severity = Severity.High
        };

        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, CancellationToken>((a, _) => a.Id = 12)
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await service.CreateAsync(request);

        Assert.False(result.DuplicateSuppressed);
        Assert.Equal(12, result.Alert.Id);
        _repository.Verify(r => r.FindActiveDuplicateAsync(
            "Service down",
            Severity.High,
            FixedNow.UtcDateTime.AddMinutes(-30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_WhenSuppressionWindowIsNonPositive_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(0));

        Assert.Equal("Alerts:DuplicateSuppressionWindowMinutes must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task AddTagsAsync_DedupesCaseInsensitive_AndReturnsSortedTags()
    {
        var existing = ExistingAlertWithTags(tags: "Ops");
        var request = new AddAlertTagsRequest { Tags = new[] { " ops ", "DB", "db" } };
        IReadOnlyCollection<string>? capturedTags = null;

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.AddTagsAsync(existing, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((_, tags, _) => capturedTags = tags)
            .ReturnsAsync(() =>
            {
                existing.Tags.Add(new Tag { Name = "DB" });
                return existing;
            });

        var result = await _service.AddTagsAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(new[] { "ops", "DB" }, capturedTags);
        Assert.Equal(new[] { "DB", "Ops" }, result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(42, new AddAlertTagsRequest { Tags = new[] { "ops" } });

        Assert.Null(result);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagIsWhitespace_ThrowsValidationException()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new[] { "   " } }));

        Assert.Equal(nameof(AddAlertTagsRequest.Tags), exception.ValidationResult!.MemberNames.Single());
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagExceedsMaxLength_ThrowsValidationException()
    {
        var existing = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new[] { new string('x', AlertConstants.TagMaxLength + 1) } }));

        Assert.Equal(nameof(AddAlertTagsRequest.Tags), exception.ValidationResult!.MemberNames.Single());
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagLimitExceeded_ThrowsValidationException()
    {
        var existing = ExistingAlertWithTags(tags: new[]
        {
            "tag-1", "tag-2", "tag-3", "tag-4", "tag-5",
            "tag-6", "tag-7", "tag-8", "tag-9", "tag-10"
        });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new[] { "tag-11" } }));

        Assert.Equal(nameof(AddAlertTagsRequest.Tags), exception.ValidationResult!.MemberNames.Single());
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task RemoveTagAsync_WhenAssignmentExists_RemovesCaseInsensitive()
    {
        var existing = ExistingAlertWithTags(1, "Ops", "Db");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, " ops ");

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(existing, "ops", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsFalse()
    {
        var existing = ExistingAlertWithTags(1, "Ops");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.RemoveTagAsync(1, "db");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
