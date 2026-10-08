using AlertService.Common.Enums;
using AlertService.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertService.Data.SQL.Tests.Repositories;

public partial class AlertRepositoryTests
{
    [Fact]
    public async Task AddTagsAsync_WhenAlertExists_PersistsTagAssignmentsAndLoadsTags()
    {
        var added = await _repository.AddAsync(NewAlert("Tagged alert", Severity.High));

        var updated = await _repository.AddTagsAsync(added.Id, ["ops", "disk"]);

        Assert.NotNull(updated);
        Assert.Equal(2, updated!.AlertTags.Count);
        Assert.Equal(2, await _context.Tags.CountAsync());
        Assert.Equal(2, await _context.AlertTags.CountAsync());
        Assert.All(updated.AlertTags, alertTag => Assert.Equal(alertTag.Tag.Value, alertTag.Tag.NormalizedValue));
    }

    [Fact]
    public async Task AddTagsAsync_WhenCalledAgainWithExistingTags_DoesNotDuplicateRelations()
    {
        var added = await _repository.AddAsync(NewAlert("Tagged alert", Severity.High));
        await _repository.AddTagsAsync(added.Id, ["ops"]);

        var updated = await _repository.AddTagsAsync(added.Id, ["ops", "disk"]);

        Assert.NotNull(updated);
        Assert.Equal(2, updated!.AlertTags.Count);
        Assert.Equal(2, await _context.Tags.CountAsync());
        Assert.Equal(2, await _context.AlertTags.CountAsync());
    }

    [Fact]
    public async Task AddTagsAsync_WhenAlertMissing_ReturnsNull()
    {
        var updated = await _repository.AddTagsAsync(999, ["ops"]);

        Assert.Null(updated);
        Assert.Empty(await _context.AlertTags.ToListAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentExists_RemovesRelationAndReturnsTrue()
    {
        var added = await _repository.AddAsync(NewAlert("Tagged alert", Severity.High));
        await _repository.AddTagsAsync(added.Id, ["ops"]);

        var removed = await _repository.RemoveTagAsync(added.Id, "ops");

        Assert.True(removed);
        Assert.Empty(await _context.AlertTags.ToListAsync());
    }

    [Fact]
    public async Task RemoveTagAsync_WhenAssignmentMissing_ReturnsFalse()
    {
        var added = await _repository.AddAsync(NewAlert("Tagged alert", Severity.High));
        await _repository.AddTagsAsync(added.Id, ["ops"]);

        var removed = await _repository.RemoveTagAsync(added.Id, "disk");

        Assert.False(removed);
        Assert.Single(await _context.AlertTags.ToListAsync());
    }

    [Fact]
    public void Model_UsesExpectedTagAndJoinConstraints()
    {
        var tagEntity = _context.Model.FindEntityType(typeof(Tag));
        var tagIndexes = tagEntity!.GetIndexes();
        Assert.Contains(tagIndexes, index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(Tag.NormalizedValue));

        var alertTagEntity = _context.Model.FindEntityType(typeof(AlertTag));
        var primaryKey = alertTagEntity!.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(
            new[] { nameof(AlertTag.AlertId), nameof(AlertTag.TagId) },
            primaryKey!.Properties.Select(property => property.Name).ToArray());
    }
}
