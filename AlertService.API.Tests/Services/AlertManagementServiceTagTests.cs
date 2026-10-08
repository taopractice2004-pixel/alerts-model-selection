using System.ComponentModel.DataAnnotations;
using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AlertService.API.Tests.Services;

public class AlertManagementServiceTagTests
{
    private readonly Mock<IAlertRepository> _repository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly AlertManagementService _service;

    public AlertManagementServiceTagTests()
    {
        _service = new AlertManagementService(
            _repository.Object,
            _timeProvider.Object,
            NullLogger<AlertManagementService>.Instance);
    }

    private static Alert ExistingAlert(int id = 1, params string[] tags)
    {
        return new Alert
        {
            Id = id,
            Title = "Memory leak",
            Description = "Heap growing",
            Severity = Severity.Medium,
            CreatedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            Tags = tags
                .Select(tag => new Tag { Name = tag, NormalizedName = tag.ToUpperInvariant() })
                .ToList()
        };
    }

    [Fact]
    public async Task GetAllAsync_PassesTagFilterToRepository_AndMapsTags()
    {
        var request = new AlertQueryRequest { Tag = "ops" };
        _repository.Setup(r => r.GetAllAsync(
                null,
                null,
                null,
                null,
                "ops",
                null,
                "createdDate",
                "desc",
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1, "Database", "Ops") }, 1));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(
            null,
            null,
            null,
            null,
            "ops",
            null,
            "createdDate",
            "desc",
            1,
            20,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(["Database", "Ops"], result.Items[0].Tags);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAlertHasTags_ReturnsTagsInResponse()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingAlert(1, "Ops", "Database"));

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(["Database", "Ops"], result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_DedupesCaseInsensitive_TrimsInput_AndReturnsUpdatedTags()
    {
        var alert = ExistingAlert(tags: "Existing");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.AddTagsAsync(alert, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Alert, IReadOnlyCollection<string>, CancellationToken>((entity, tagNames, _) =>
            {
                foreach (var tagName in tagNames)
                {
                    entity.Tags.Add(new Tag
                    {
                        Name = tagName,
                        NormalizedName = tagName.Trim().ToUpperInvariant()
                    });
                }
            })
            .Returns(Task.CompletedTask);

        var result = await _service.AddTagsAsync(1, new AddAlertTagsRequest
        {
            Tags = ["  Ops  ", "ops", "Database"]
        });

        _repository.Verify(r => r.AddTagsAsync(
            alert,
            It.Is<IReadOnlyCollection<string>>(tags => tags.SequenceEqual(new[] { "Ops", "Database" })),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(["Database", "Existing", "Ops"], result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertWouldExceedMaxTags_ThrowsValidationException()
    {
        var existingTags = Enumerable.Range(1, AlertConstants.MaxTagsPerAlert)
            .Select(index => $"Tag{index}")
            .ToArray();
        var alert = ExistingAlert(tags: existingTags);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddTagsAsync(1, new AddAlertTagsRequest
        {
            Tags = ["Overflow"]
        }));

        _repository.Verify(r => r.AddTagsAsync(It.IsAny<Alert>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_ReturnsUpdatedResponse()
    {
        var alert = ExistingAlert(1, "Ops", "Database");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "OPS", It.IsAny<CancellationToken>()))
            .Callback<Alert, string, CancellationToken>((entity, normalizedTag, _) =>
            {
                var existingTag = entity.Tags.Single(tag => tag.NormalizedName == normalizedTag);
                entity.Tags.Remove(existingTag);
            })
            .ReturnsAsync(true);

        var result = await _service.RemoveTagAsync(1, " ops ");

        Assert.NotNull(result);
        Assert.Equal(["Database"], result!.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsNull()
    {
        var alert = ExistingAlert(tags: "Database");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _repository.Setup(r => r.RemoveTagAsync(alert, "OPS", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RemoveTagAsync(1, "Ops");

        Assert.Null(result);
    }
}