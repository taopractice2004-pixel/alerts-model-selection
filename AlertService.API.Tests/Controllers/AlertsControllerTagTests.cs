using System.ComponentModel.DataAnnotations;
using AlertService.API.Controllers;
using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertService.API.Tests.Controllers;

public class AlertsControllerTagTests
{
    private readonly Mock<IAlertService> _service = new();
    private readonly AlertsController _controller;

    public AlertsControllerTagTests()
    {
        _controller = new AlertsController(_service.Object);
    }

    private static AlertResponse SampleResponse(int id = 1, params string[] tags)
    {
        return new AlertResponse
        {
            Id = id,
            Title = "Disk usage high",
            Description = "85% used",
            Severity = Severity.High,
            CreatedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            Tags = tags
        };
    }

    [Fact]
    public async Task GetAll_PassesTagFilterToService()
    {
        var request = new AlertQueryRequest { Tag = "ops" };
        _service.Setup(s => s.GetAllAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<AlertResponse>());

        _ = await _controller.GetAll(request, CancellationToken.None);

        _service.Verify(s => s.GetAllAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTags_WhenFound_ReturnsOkWithUpdatedAlert()
    {
        var request = new AddAlertTagsRequest { Tags = ["Ops", "Database"] };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleResponse(1, "Database", "Ops"));

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(["Database", "Ops"], body.Tags);
    }

    [Fact]
    public async Task AddTags_WhenMissing_ReturnsNotFound()
    {
        var request = new AddAlertTagsRequest { Tags = ["Ops"] };
        _service.Setup(s => s.AddTagsAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertResponse?)null);

        var result = await _controller.AddTags(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task RemoveTag_WhenFound_ReturnsOkWithUpdatedAlert()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleResponse(1, "Database"));

        var result = await _controller.RemoveTag(1, "Ops", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(["Database"], body.Tags);
    }

    [Fact]
    public async Task RemoveTag_WhenMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(99, "Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertResponse?)null);

        var result = await _controller.RemoveTag(99, "Ops", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void AddAlertTagsRequest_WithInvalidValues_FailsValidation()
    {
        var request = new AddAlertTagsRequest
        {
            Tags = [" ", new string('x', AlertConstants.TagMaxLength + 1)]
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Tags[0]"));
        Assert.Contains(results, r => r.MemberNames.Contains("Tags[1]"));
    }

    [Fact]
    public void AddAlertTagsRequest_WithEmptyCollection_FailsValidation()
    {
        var request = new AddAlertTagsRequest();
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AddAlertTagsRequest_WithNullCollection_FailsValidation()
    {
        var request = new AddAlertTagsRequest { Tags = null! };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public void AlertQueryRequest_WithInvalidTag_FailsValidation(string tag)
    {
        var request = new AlertQueryRequest { Tag = tag };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }

    [Fact]
    public void AlertQueryRequest_WithTagLongerThanLimit_FailsValidation()
    {
        var request = new AlertQueryRequest { Tag = new string('x', AlertConstants.TagMaxLength + 1) };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }
}