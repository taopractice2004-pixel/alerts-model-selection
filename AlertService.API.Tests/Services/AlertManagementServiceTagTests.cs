using AlertService.Common.Constants;
using AlertService.Models;
using Moq;

namespace AlertService.API.Tests.Services;

public partial class AlertManagementServiceTests
{
    [Fact]
    public async Task AddTagsAsync_WhenAlertExists_NormalizesDistinctTags_AndReturnsUpdatedAlert()
    {
        var existing = ExistingAlertWithTags(1, "existing");
        var updated = ExistingAlertWithTags(1, "existing", "ops", "disk");
        var inputTags = new List<string> { " Ops ", "ops", "DISK" };

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.AddTagsAsync(
                1,
                It.Is<IReadOnlyCollection<string>>(tags => tags.Count == 2 && tags.Contains("ops") && tags.Contains("disk")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _service.AddTagsAsync(1, inputTags);

        Assert.NotNull(result);
        Assert.Equal(new[] { "disk", "existing", "ops" }, result!.Tags);
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Alert?)null);

        var result = await _service.AddTagsAsync(99, new List<string> { "ops" });

        Assert.Null(result);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenFinalCountExceedsMax_ThrowsArgumentException()
    {
        var existingTags = Enumerable.Range(1, AlertConstants.MaxTagsPerAlert)
            .Select(index => $"tag{index}")
            .ToArray();
        var existing = ExistingAlertWithTags(1, existingTags);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTagsAsync(1, new List<string> { "extra" }));

        Assert.Contains($"at most {AlertConstants.MaxTagsPerAlert}", ex.Message);
        _repository.Verify(r => r.AddTagsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTagsAsync_WhenTagLengthInvalid_ThrowsArgumentException()
    {
        var existing = ExistingAlertWithTags(1, "ops");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTagsAsync(1, new List<string> { new string('x', AlertConstants.TagMaxLength + 1) }));
    }

    [Fact]
    public async Task RemoveTagAsync_NormalizesTagBeforeRepositoryCall()
    {
        _repository.Setup(r => r.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var removed = await _service.RemoveTagAsync(1, " Ops ");

        Assert.True(removed);
        _repository.Verify(r => r.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagWhitespace_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemoveTagAsync(1, "   "));
    }

    [Fact]
    public async Task RemoveTagAsync_WhenTagTooLong_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemoveTagAsync(1, new string('x', AlertConstants.TagMaxLength + 1)));
    }
}
