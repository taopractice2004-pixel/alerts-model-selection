using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertService.API.Tests.Controllers;

public partial class AlertsControllerTests
{
    [Fact]
    public async Task AddTags_WhenValid_ReturnsOkWithUpdatedAlert()
    {
        var tags = new List<string> { "Ops", "Disk" };
        _service.Setup(s => s.AddTagsAsync(1, tags, It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse(1));

        var result = await _controller.AddTags(1, tags, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(1, body.Id);
        Assert.Equal(2, body.Tags.Count);
    }

    [Fact]
    public async Task AddTags_WhenAlertMissing_ReturnsNotFound()
    {
        var tags = new List<string> { "ops" };
        _service.Setup(s => s.AddTagsAsync(99, tags, It.IsAny<CancellationToken>())).ReturnsAsync((AlertResponse?)null);

        var result = await _controller.AddTags(99, tags, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTags_WhenServiceThrowsArgumentException_ReturnsBadRequestValidationProblem()
    {
        var tags = new List<string> { new string('x', 31) };
        _service.Setup(s => s.AddTagsAsync(1, tags, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Each tag length must be between 1 and 30 characters."));

        var result = await _controller.AddTags(1, tags, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.True(details.Errors.ContainsKey("tags"));
    }

    [Fact]
    public async Task AddTags_WhenTagsEmpty_ReturnsBadRequestValidationProblem()
    {
        var result = await _controller.AddTags(1, [], CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.True(details.Errors.ContainsKey("tags"));
    }

    [Fact]
    public async Task RemoveTag_WhenTagIsInvalid_ReturnsBadRequest()
    {
        var result = await _controller.RemoveTag(1, "   ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task RemoveTag_WhenAssignmentExists_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.RemoveTag(1, "ops", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTag_WhenAssignmentMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.RemoveTag(1, "ops", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
