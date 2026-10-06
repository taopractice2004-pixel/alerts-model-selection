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
        _service = CreateService(15);
    }

    private AlertManagementService CreateService(int windowMinutes) => new(
        _repository.Object,
        _timeProvider.Object,
        Options.Create(new DuplicateSuppressionOptions { DuplicateWindowMinutes = windowMinutes }),
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
            Tag = "Prod",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "Prod", "title", "asc", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(true, Severity.Critical, request.CreatedFrom, request.CreatedTo, "disk", "Prod", "title", "asc", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.False(result.DuplicateSuppressed);
        Assert.Equal(10, result.Alert.Id);
        Assert.Equal(Severity.Critical, result.Alert.Severity);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicateInWindow_ReturnsExisting_AndDoesNotInsert()
    {
        var existing = ExistingAlert(7);
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync("Memory leak", Severity.Medium, FixedNow.UtcDateTime.AddMinutes(-15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "  Memory leak ", Severity = Severity.Medium });

        Assert.True(result.DuplicateSuppressed);
        Assert.Equal(7, result.Alert.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNoDuplicate_InsertsAndIsNotSuppressed()
    {
        _repository.Setup(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "New one", Severity = Severity.Low });

        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    public async Task CreateAsync_UsesConfiguredWindowForLookup(int windowMinutes)
    {
        var service = CreateService(windowMinutes);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        await service.CreateAsync(new CreateAlertRequest { Title = "x", Severity = Severity.High });

        _repository.Verify(r => r.FindRecentActiveDuplicateAsync("x", Severity.High, FixedNow.UtcDateTime.AddMinutes(-windowMinutes), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenWindowIsZero_SkipsLookup_AndInserts()
    {
        var service = CreateService(0);
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await service.CreateAsync(new CreateAlertRequest { Title = "x", Severity = Severity.High });

        Assert.False(result.DuplicateSuppressed);
        _repository.Verify(r => r.FindRecentActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private static Alert AlertWithTags(params string[] names)
    {
        var alert = ExistingAlert();
        foreach (var name in names)
        {
            alert.Tags.Add(new Tag { Name = name, NormalizedName = Tag.Normalize(name) });
        }

        return alert;
    }

    // Mimics the repository: attaches the tags to the loaded alert.
    private void SetupAddTagsToAttach()
    {
        _repository.Setup(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((alert, names, _) =>
            {
                foreach (var name in names)
                {
                    alert.Tags.Add(new Tag { Name = name, NormalizedName = Tag.Normalize(name) });
                }
            })
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTagNamesSortedCaseInsensitively()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AlertWithTags("prod", "Beta", "alpha"));

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(new[] { "alpha", "Beta", "prod" }, result!.Tags);
    }

    [Fact]
    public async Task GetAllAsync_IncludesTagsInResponses()
    {
        _repository.Setup(r => r.GetAllAsync(null, null, null, null, null, "prod", "createdDate", "desc", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { AlertWithTags("prod", "db") }, 1));

        var result = await _service.GetAllAsync(new AlertQueryRequest { Tag = "prod" });

        Assert.Equal(new[] { "db", "prod" }, result.Items.Single().Tags);
    }

    [Fact]
    public async Task CreateAsync_ResponseHasEmptyTags()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert a, CancellationToken _) => a);

        var result = await _service.CreateAsync(new CreateAlertRequest { Title = "x", Severity = Severity.Low });

        Assert.Empty(result.Alert.Tags);
    }

    [Fact]
    public async Task UpdateAsync_ResponseIncludesExistingTags()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("prod"));

        var result = await _service.UpdateAsync(1, new UpdateAlertRequest { Title = "x", Severity = Severity.Low });

        Assert.Equal(new[] { "prod" }, result!.Tags);
    }

    [Fact]
    public async Task DeactivateAsync_ResponseIncludesExistingTags()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("prod"));

        var result = await _service.DeactivateAsync(1);

        Assert.Equal(new[] { "prod" }, result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(9, new AddAlertTagsRequest { Tags = new() { "prod" } });

        Assert.Equal(AddAlertTagsOutcome.NotFound, result.Outcome);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTagsAsync(1, null!));
    }

    [Fact]
    public async Task AddTagsAsync_AddsTrimmedTags_AndReturnsThemInResponse()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());
        SetupAddTagsToAttach();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "  Prod ", "db" } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
        Assert.Equal(new[] { "db", "Prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(
            It.IsAny<Alert>(),
            It.Is<IReadOnlyCollection<string>>(n => n.SequenceEqual(new[] { "Prod", "db" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesCaseInsensitivelyWithinRequest_KeepingFirstCasing()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());
        SetupAddTagsToAttach();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "Prod", "PROD", "prod " } });

        Assert.Equal(new[] { "Prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(
            It.IsAny<Alert>(),
            It.Is<IReadOnlyCollection<string>>(n => n.SequenceEqual(new[] { "Prod" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_SkipsTagsAlreadyOnAlert_CaseInsensitively()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("Prod"));
        SetupAddTagsToAttach();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "PROD", "db" } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
        Assert.Equal(new[] { "db", "Prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(
            It.IsAny<Alert>(),
            It.Is<IReadOnlyCollection<string>>(n => n.SequenceEqual(new[] { "db" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAllTagsAlreadyAssigned_IsIdempotentAndDoesNotPersist()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("Prod"));

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "prod" } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
        Assert.Equal(new[] { "Prod" }, result.Alert!.Tags);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task AddTagsAsync_WithEmptyOrWhitespaceTag_ReturnsInvalid_AndPersistsNothing(string tag)
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "ok", tag } });

        Assert.Equal(AddAlertTagsOutcome.Invalid, result.Outcome);
        Assert.False(string.IsNullOrEmpty(result.Error));
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WithNullTagEntry_ReturnsInvalid()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { null! } });

        Assert.Equal(AddAlertTagsOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task AddTagsAsync_WithTagLongerThan30AfterTrim_ReturnsInvalid_AndPersistsNothing()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "ok", new string('x', 31) } });

        Assert.Equal(AddAlertTagsOutcome.Invalid, result.Outcome);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WithBoundaryLengths1And30_Succeeds()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());
        SetupAddTagsToAttach();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "a", $"  {new string('b', 30)}  " } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
        Assert.Equal(2, result.Alert!.Tags.Count);
    }

    [Fact]
    public async Task AddTagsAsync_WithEmptyTagList_ReturnsInvalid()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAlert());

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() });

        Assert.Equal(AddAlertTagsOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultWouldExceed10Tags_ReturnsInvalid_AndPersistsNothing()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 9).Select(i => $"t{i}").ToArray());
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "new1", "new2" } });

        Assert.Equal(AddAlertTagsOutcome.Invalid, result.Outcome);
        Assert.Equal(9, alert.Tags.Count);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenResultReachesExactly10Tags_Succeeds()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 9).Select(i => $"t{i}").ToArray());
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        SetupAddTagsToAttach();

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "new1" } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
        Assert.Equal(10, result.Alert!.Tags.Count);
    }

    [Fact]
    public async Task AddTagsAsync_DuplicatesDoNotCountTowardsLimit()
    {
        var alert = AlertWithTags(Enumerable.Range(1, 10).Select(i => $"t{i}").ToArray());
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest { Tags = new() { "T1", "t2" } });

        Assert.Equal(AddAlertTagsOutcome.Success, result.Outcome);
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
    [InlineData("")]
    [InlineData("  ")]
    public async Task RemoveTagAsync_WithBlankTag_ReturnsFalse(string tag)
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(AlertWithTags("db"));

        var result = await _service.RemoveTagAsync(1, tag);

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTagAsync_WithOverLengthTag_ReturnsFalse_WithoutLookupOrRemoval()
    {
        var tag = new string('a', AlertService.Common.Constants.AlertConstants.TagMaxLength + 1);

        var result = await _service.RemoveTagAsync(1, tag);

        Assert.False(result);
        _repository.Verify(r => r.RemoveTagAsync(It.IsAny<Alert>(), It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("PROD")]
    [InlineData("  Prod  ")]
    public async Task RemoveTagAsync_WhenAssigned_MatchesCaseInsensitively_AndRemoves(string tag)
    {
        var alert = AlertWithTags("Prod", "db");
        var assigned = alert.Tags.First(t => t.Name == "Prod");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _service.RemoveTagAsync(1, tag);

        Assert.True(result);
        _repository.Verify(r => r.RemoveTagAsync(alert, assigned, It.IsAny<CancellationToken>()), Times.Once);
    }
}
