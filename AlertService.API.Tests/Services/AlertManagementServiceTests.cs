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
    private readonly AlertSuppressionOptions _suppressionOptions = new() { DuplicateWindowMinutes = 15 };
    private readonly AlertManagementService _service;

    public AlertManagementServiceTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(FixedNow);
        _repository.Setup(r => r.FindActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            Microsoft.Extensions.Options.Options.Create(_suppressionOptions),
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
    public async Task GetTrendsAsync_WithDefaultDays_ReturnsSevenOldestFirstZeroFilledBuckets()
    {
        _repository.Setup(r => r.GetTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, Severity Severity, int Count)>());

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest());

        var today = FixedNow.UtcDateTime.Date;
        var expectedFrom = today.AddDays(-6);
        _repository.Verify(r => r.GetTrendsAsync(expectedFrom, today, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(expectedFrom, result.Buckets[0].Date);
        Assert.Equal(today, result.Buckets[^1].Date);
        Assert.All(result.Buckets, b =>
        {
            Assert.Equal(0, b.TotalCount);
            Assert.Equal(0, b.SeverityCounts.Low);
            Assert.Equal(0, b.SeverityCounts.Medium);
            Assert.Equal(0, b.SeverityCounts.High);
            Assert.Equal(0, b.SeverityCounts.Critical);
        });
    }

    [Fact]
    public async Task GetTrendsAsync_WithExplicitDays_ComputesCorrectWindow()
    {
        _repository.Setup(r => r.GetTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, Severity Severity, int Count)>());

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        var today = FixedNow.UtcDateTime.Date;
        var expectedFrom = today.AddDays(-2);
        _repository.Verify(r => r.GetTrendsAsync(expectedFrom, today, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(3, result.Buckets.Count);
    }

    [Fact]
    public async Task GetTrendsAsync_FillsMissingDaysAndSeverities_AndSumsMatchingSeverityRows()
    {
        var today = FixedNow.UtcDateTime.Date;
        var dayWithData = today.AddDays(-1);

        _repository.Setup(r => r.GetTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DateTime Date, Severity Severity, int Count)>
            {
                (dayWithData, Severity.High, 2),
                (dayWithData, Severity.Critical, 1),
                (today, Severity.Low, 5)
            });

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        Assert.Equal(3, result.Buckets.Count);

        var empty = result.Buckets.Single(b => b.Date == today.AddDays(-2));
        Assert.Equal(0, empty.TotalCount);

        var mixed = result.Buckets.Single(b => b.Date == dayWithData);
        Assert.Equal(3, mixed.TotalCount);
        Assert.Equal(0, mixed.SeverityCounts.Low);
        Assert.Equal(0, mixed.SeverityCounts.Medium);
        Assert.Equal(2, mixed.SeverityCounts.High);
        Assert.Equal(1, mixed.SeverityCounts.Critical);

        var lowOnly = result.Buckets.Single(b => b.Date == today);
        Assert.Equal(5, lowOnly.TotalCount);
        Assert.Equal(5, lowOnly.SeverityCounts.Low);
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
    public async Task CreateAsync_WhenNoDuplicateExists_ReturnsCreatedStatus_AndChecksRepositoryWithConfiguredCutoff()
    {
        var request = new CreateAlertRequest { Title = "New issue", Severity = Severity.High };
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 20; return a; });

        var result = await _service.CreateAsync(request);

        Assert.Equal(CreateAlertStatus.Created, result.Status);
        var expectedCutoff = FixedNow.UtcDateTime - TimeSpan.FromMinutes(_suppressionOptions.DuplicateWindowMinutes);
        _repository.Verify(r => r.FindActiveDuplicateAsync("New issue", Severity.High, expectedCutoff, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateWithinWindowExists_SuppressesCreation_AndReturnsExistingAlert()
    {
        var existing = ExistingAlert(99);
        existing.Title = "Memory leak";
        existing.Severity = Severity.Medium;
        existing.IsActive = true;

        _repository.Setup(r => r.FindActiveDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Medium };

        var result = await _service.CreateAsync(request);

        Assert.Equal(CreateAlertStatus.DuplicateSuppressed, result.Status);
        Assert.Equal(99, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryReportsNoDuplicateForDifferentSeverity_CreatesNewAlert()
    {
        // The repository is responsible for the severity match; this test confirms the service
        // trusts a "no duplicate" result (e.g. because severities differ) and proceeds to create.
        _repository.Setup(r => r.FindActiveDuplicateAsync("Memory leak", Severity.Critical, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 30; return a; });

        var request = new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Critical };

        var result = await _service.CreateAsync(request);

        Assert.Equal(CreateAlertStatus.Created, result.Status);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryReportsNoDuplicateForInactiveMatch_CreatesNewAlert()
    {
        // The repository excludes inactive matches; this test confirms the service trusts a
        // "no duplicate" result and proceeds to create a new alert.
        _repository.Setup(r => r.FindActiveDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 31; return a; });

        var request = new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Medium };

        var result = await _service.CreateAsync(request);

        Assert.Equal(CreateAlertStatus.Created, result.Status);
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
