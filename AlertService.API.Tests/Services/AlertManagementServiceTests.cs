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
        _service = CreateService(15);
    }

    private AlertManagementService CreateService(int windowMinutes) => new(
        _repository.Object,
        _timeProvider.Object,
        Options.Create(new AlertSuppressionOptions { DuplicateWindowMinutes = windowMinutes }),
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
            Tag = "prod",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "prod", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "prod", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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

    private static readonly DateTime FixedToday = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private void SetupDailyCounts(params (DateTime Date, Severity Severity, int Count)[] rows) =>
        _repository.Setup(r => r.GetDailySeverityCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    [Fact]
    public async Task GetTrendsAsync_WithNoAlerts_ReturnsZeroFilledBucketsOldestFirstEndingToday()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        Assert.Equal(3, result.Days);
        Assert.Equal(
            new[] { new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1) },
            result.Buckets.Select(b => b.Date));
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
    public async Task GetTrendsAsync_DefaultRequest_ReturnsSevenBuckets()
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest());

        Assert.Equal(AlertConstants.DefaultTrendDays, result.Days);
        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(new DateOnly(2026, 8, 26), result.Buckets[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
    }

    [Theory]
    [InlineData(AlertConstants.MinTrendDays)]
    [InlineData(AlertConstants.MaxTrendDays)]
    public async Task GetTrendsAsync_AtBoundaryDays_ReturnsThatManyBuckets(int days)
    {
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = days });

        Assert.Equal(days, result.Buckets.Count);
        Assert.Equal(DateOnly.FromDateTime(FixedToday.AddDays(-(days - 1))), result.Buckets[0].Date);
        Assert.Equal(DateOnly.FromDateTime(FixedToday), result.Buckets[^1].Date);
    }

    [Fact]
    public async Task GetTrendsAsync_MapsCountsToDaysAndSeverities_AndTotalsTheirSum()
    {
        SetupDailyCounts(
            (FixedToday.AddDays(-2), Severity.Low, 2),
            (FixedToday.AddDays(-2), Severity.Critical, 1),
            (FixedToday, Severity.Medium, 3),
            (FixedToday, Severity.High, 4));

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 3 });

        var first = result.Buckets[0];
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.SeverityCounts.Low);
        Assert.Equal(0, first.SeverityCounts.Medium);
        Assert.Equal(0, first.SeverityCounts.High);
        Assert.Equal(1, first.SeverityCounts.Critical);

        var middle = result.Buckets[1];
        Assert.Equal(0, middle.TotalCount);

        var last = result.Buckets[2];
        Assert.Equal(7, last.TotalCount);
        Assert.Equal(0, last.SeverityCounts.Low);
        Assert.Equal(3, last.SeverityCounts.Medium);
        Assert.Equal(4, last.SeverityCounts.High);
        Assert.Equal(0, last.SeverityCounts.Critical);
    }

    [Fact]
    public async Task GetTrendsAsync_QueriesRepositoryForWindowStartInclusiveToTomorrowExclusive()
    {
        SetupDailyCounts();

        await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 7 });

        _repository.Verify(r => r.GetDailySeverityCountsAsync(
            FixedToday.AddDays(-6),
            FixedToday.AddDays(1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrendsAsync_WhenNowIsLateInUtcDay_StillUsesCurrentUtcDayAsLastBucket()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(2026, 9, 1, 23, 59, 59, TimeSpan.Zero));
        SetupDailyCounts();

        var result = await _service.GetTrendsAsync(new AlertTrendsQueryRequest { Days = 2 });

        Assert.Equal(new DateOnly(2026, 9, 1), result.Buckets[^1].Date);
        Assert.Equal(new DateOnly(2026, 8, 31), result.Buckets[0].Date);
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
        Assert.False(result.IsDuplicate);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRecentActiveDuplicateExists_ReturnsExistingAlert_AndDoesNotInsert()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync("Memory leak", Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "  Memory leak  ", Severity = Severity.Medium });

        Assert.True(result.IsDuplicate);
        Assert.Equal(7, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_InsertsAndReturnsNotDuplicate()
    {
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => { a.Id = 3; return a; });

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "Disk full", Severity = Severity.High });

        Assert.False(result.IsDuplicate);
        Assert.Equal(3, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(5)]
    [InlineData(60)]
    public async Task CreateAsync_PassesWindowStartFromConfiguredMinutes_ToRepository(int windowMinutes)
    {
        var service = CreateService(windowMinutes);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingAlert());

        await service.CreateAsync(new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Medium });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(
            "Memory leak",
            Severity.Medium,
            FixedNow.UtcDateTime.AddMinutes(-windowMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PassesRequestSeverityToDuplicateLookup()
    {
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await _service.CreateAsync(new CreateAlertRequest { Title = "Memory leak", Severity = Severity.Critical });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync("Memory leak", Severity.Critical, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), Severity.Medium, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private static Alert AlertWithTags(params string[] names)
    {
        var alert = ExistingAlert();
        var id = 1;
        foreach (var name in names)
        {
            alert.Tags.Add(new Tag { Id = id++, Name = name });
        }

        return alert;
    }

    private void SetupAddTags(Alert alert, List<string>? captured = null)
    {
        _repository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((a, names, _) =>
            {
                captured?.AddRange(names);
                var nextId = a.Tags.Count + 1;
                foreach (var name in names)
                {
                    a.Tags.Add(new Tag { Id = nextId++, Name = name });
                }
            })
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetAllAsync_WithTag_PassesTagToRepository()
    {
        _repository.Setup(r => r.GetAllAsync(null, null, null, null, null, "prod", "createdDate", "desc", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { AlertWithTags("prod") }, 1));

        var result = await _service.GetAllAsync(new AlertQueryRequest { Tag = "prod" });

        Assert.Single(result.Items);
        Assert.Equal(new[] { "prod" }, result.Items[0].Tags);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTagsOrderedByName()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("zeta", "Alpha", "beta"));

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(new[] { "Alpha", "beta", "zeta" }, result!.Tags);
    }

    [Fact]
    public async Task CreateAsync_ResponseHasEmptyTags()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "x", Severity = Severity.Low });

        Assert.NotNull(result.Alert.Tags);
        Assert.Empty(result.Alert.Tags);
    }

    [Fact]
    public async Task UpdateAsync_ResponseIncludesTags()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("prod"));

        var result = await _service.UpdateAsync(1, new UpdateAlertRequest { Title = "x", Severity = Severity.Low });

        Assert.Equal(new[] { "prod" }, result!.Tags);
    }

    [Fact]
    public async Task DeactivateAsync_ResponseIncludesTags()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("prod"));

        var result = await _service.DeactivateAsync(1);

        Assert.Equal(new[] { "prod" }, result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsAlertNotFound_AndDoesNotSave()
    {
        _repository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(9, new AddTagsRequest { Tags = { "a" } });

        Assert.Equal(AddTagsOutcome.AlertNotFound, result.Outcome);
        Assert.Null(result.Alert);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_AddsTrimmedTags_AndReturnsUpdatedAlert()
    {
        var alert = ExistingAlert();
        var captured = new List<string>();
        SetupAddTags(alert, captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "  prod ", "db" } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(new[] { "prod", "db" }, captured);
        Assert.Equal(new[] { "db", "prod" }, result.Alert!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesWithinRequest_CaseInsensitively_KeepingFirstCasing()
    {
        var alert = ExistingAlert();
        var captured = new List<string>();
        SetupAddTags(alert, captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "Prod", "prod", " PROD " } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(new[] { "Prod" }, captured);
        Assert.Equal(new[] { "Prod" }, result.Alert!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_TagAlreadyOnAlert_IsNotAddedTwice()
    {
        var alert = AlertWithTags("Prod");
        var captured = new List<string>();
        SetupAddTags(alert, captured);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "prod", "db" } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(new[] { "db" }, captured);
        Assert.Equal(new[] { "db", "Prod" }, result.Alert!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_OnlyDuplicates_DoesNotCallRepository_AndReturnsAdded()
    {
        var alert = AlertWithTags("prod");
        SetupAddTags(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "PROD" } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(new[] { "prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_ReachingExactlyTenTags_Succeeds()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 8).Select(i => $"t{i}").ToArray());
        SetupAddTags(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "n1", "n2" } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(10, result.Alert!.Tags.Count);
    }

    [Fact]
    public async Task AddTagsAsync_ExceedingTenTags_ReturnsLimitExceeded_AndAddsNothing()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 9).Select(i => $"t{i}").ToArray());
        SetupAddTags(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "n1", "n2" } });

        Assert.Equal(AddTagsOutcome.TagLimitExceeded, result.Outcome);
        Assert.Null(result.Alert);
        Assert.Equal(9, alert.Tags.Count);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_DuplicateOfExistingTag_DoesNotCountTowardLimit()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 10).Select(i => $"t{i}").ToArray());
        SetupAddTags(alert);

        var result = await _service.AddTagsAsync(1, new AddTagsRequest { Tags = { "T1" } });

        Assert.Equal(AddTagsOutcome.Added, result.Outcome);
        Assert.Equal(10, result.Alert!.Tags.Count);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAlertMissing_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.RemoveTagAsync(9, "prod");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagNotAssigned_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("db"));

        var result = await _service.RemoveTagAsync(1, "prod");

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("PROD")]
    [InlineData("  Prod  ")]
    public async Task RemoveTagAsync_WhenAssigned_RemovesCaseInsensitively_AndReturnsTrue(string requested)
    {
        var alert = AlertWithTags("Prod", "db");
        var assigned = alert.Tags.First(t => t.Name == "Prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, requested);

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(alert, assigned, It.IsAny<CancellationToken>()), Times.Once);
    }
}
