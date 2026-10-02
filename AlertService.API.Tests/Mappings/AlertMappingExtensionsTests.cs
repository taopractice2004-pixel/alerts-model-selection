using AlertService.API.Mappings;
using AlertService.Common.Enums;
using AlertService.Models;

namespace AlertService.API.Tests.Mappings;

public class AlertMappingExtensionsTests
{
    private static Alert NewAlert(params string[] tagNames)
    {
        var alert = new Alert
        {
            Id = 1,
            Title = "Disk full",
            Severity = Severity.High,
            CreatedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        foreach (var name in tagNames)
        {
            alert.Tags.Add(new Tag { Name = name });
        }

        return alert;
    }

    [Fact]
    public void ToResponse_MapsTagsSortedAscending()
    {
        var response = NewAlert("network", "cpu", "disk").ToResponse();

        Assert.Equal(new[] { "cpu", "disk", "network" }, response.Tags);
    }

    [Fact]
    public void ToResponse_WithNoTags_ReturnsEmptyList()
    {
        var response = NewAlert().ToResponse();

        Assert.NotNull(response.Tags);
        Assert.Empty(response.Tags);
    }
}
