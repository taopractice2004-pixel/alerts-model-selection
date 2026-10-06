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

    private const int DefaultWindowMinutes = 15;

    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _service = CreateService(DefaultWindowMinutes);
    }

    private AlertManagementService CreateService(int windowMinutes) => new(
        _repository.Object,
        _timeProvider.Object,
        Options.Create(new AlertSuppressionOptions { WindowMinutes = windowMinutes }),
        NullLogger<AlertManagementService>.Instance);

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
            Tag = "database",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "database", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "database", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Equal(CreateAlertStatus.Created, result.Status);
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
    public async Task CreateAsync_WhenRecentActiveDuplicateExists_SuppressesAndReturnsExisting()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindRecentDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateAlertRequest { Title = "Memory leak", Description = "Heap growing again", Severity = Severity.Medium, IsActive = true };

        var result = await _service.CreateAsync(request);

        Assert.Equal(CreateAlertStatus.Suppressed, result.Status);
        Assert.Equal(7, result.Alert.Id);
        Assert.Equal("Memory leak", result.Alert.Title);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_CreatesAndReturnsCreatedStatus()
    {
        _repository.Setup(r => r.FindRecentDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 11; return a; });

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        Assert.Equal(CreateAlertStatus.Created, result.Status);
        Assert.Equal(11, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ForwardsRequestTitleAndSeverity_ToDuplicateLookup()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 1; return a; });

        await _service.CreateAsync(new CreateAlertRequest { Title = "  Disk full  ", Severity = Severity.Critical });

        _repository.Verify(r => r.FindRecentDuplicateAsync("  Disk full  ", Severity.Critical, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredWindow_ToComputeCutoffFromTimeProvider()
    {
        DateTime capturedCutoff = default;
        _repository.Setup(r => r.FindRecentDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, Severity, DateTime, CancellationToken>((_, _, cutoff, _) => capturedCutoff = cutoff)
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 1; return a; });

        var service = CreateService(windowMinutes: 30);
        await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.Low });

        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(-30), capturedCutoff);
    }

    [Fact]
    public async Task CreateAsync_WhenWindowDisabled_SkipsDuplicateLookup_AndCreates()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 3; return a; });

        var service = CreateService(windowMinutes: 0);
        var result = await service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.Low });

        Assert.Equal(CreateAlertStatus.Created, result.Status);
        _repository.Verify(r => r.FindRecentDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private void SetupAddTagsAppendsToAlert(List<string>? captured = null)
    {
        _repository.Setup(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((alert, names, _) =>
            {
                captured?.AddRange(names);
                foreach (var name in names)
                {
                    alert.Tags.Add(new Tag { Name = name });
                }
            })
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(99, new AddTagsRequest { Tags = new[] { "prod" } });

        Assert.Equal(AddTagsStatus.AlertNotFound, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WithNewTags_AddsThemAndReturnsUpdatedAlert()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        var captured = new List<string>();
        SetupAddTagsAppendsToAlert(captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "prod", "database" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Equal(new[] { "database", "prod" }, result.Alert!.Tags); // AlertResponse sorts case-insensitively
        Assert.Equal(new[] { "prod", "database" }, captured);
        _repository.Verify(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_TrimsAndDropsBlankTags()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        var captured = new List<string>();
        SetupAddTagsAppendsToAlert(captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "  prod  ", "   ", "" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        Assert.Equal(new[] { "prod" }, captured);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesWithinRequestCaseInsensitively()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        var captured = new List<string>();
        SetupAddTagsAppendsToAlert(captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "prod", "PROD", "Prod" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        Assert.Single(captured);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagAlreadyAssigned_IsNoOp()
    {
        var alert = ExistingAlert();
        alert.Tags.Add(new Tag { Name = "Prod" });
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "prod" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        Assert.NotNull(result.Alert);
        Assert.Equal(new[] { "Prod" }, result.Alert!.Tags); // first-seen casing preserved
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultWouldExceedMax_ReturnsTagLimitExceeded()
    {
        var alert = ExistingAlert();
        for (var i = 0; i < 10; i++)
        {
            alert.Tags.Add(new Tag { Name = $"tag{i}" });
        }
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "eleven" } });

        Assert.Equal(AddTagsStatus.TagLimitExceeded, result.Status);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_FillingExactlyToMax_Succeeds()
    {
        var alert = ExistingAlert();
        for (var i = 0; i < 8; i++)
        {
            alert.Tags.Add(new Tag { Name = $"tag{i}" });
        }
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        SetupAddTagsAppendsToAlert();

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = new[] { "nine", "ten" } });

        Assert.Equal(AddTagsStatus.Success, result.Status);
        _repository.Verify(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(99, "prod");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagAssigned_TrimsAndReturnsTrue()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "prod", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RemoveTagAsync(1, "  prod  ");

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(alert, "prod", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse()
    {
        var alert = ExistingAlert();
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "missing", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RemoveTagAsync(1, "missing");

        Assert.False(result);
    }
}
