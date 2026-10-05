using System.ComponentModel.DataAnnotations;
using AlertService.API.Controllers;
using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace AlertService.API.Tests.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _service = new();
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        var problemDetailsFactory = new Mock<ProblemDetailsFactory>();
        problemDetailsFactory
            .Setup(f => f.CreateValidationProblemDetails(
                It.IsAny<HttpContext>(),
                It.IsAny<ModelStateDictionary>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns((HttpContext _, ModelStateDictionary ms, int? _, string? _, string? _, string? _, string? _) =>
                new ValidationProblemDetails(ms) { Status = StatusCodes.Status400BadRequest });

        _controller = new AlertsController(_service.Object) { ProblemDetailsFactory = problemDetailsFactory.Object };
    }

    private static PagedResponse<AlertResponse> SamplePagedResponse(params AlertResponse[] items) => new()
    {
        Items = items,
        Page = 1,
        PageSize = 20,
        TotalCount = items.Length,
        TotalPages = items.Length == 0 ? 0 : 1
    };

    private static AlertResponse SampleResponse(int id = 1) => new()
    {
        Id = id,
        Title = "Disk usage high",
        Description = "85% used",
        Severity = Severity.High,
        CreatedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        IsActive = true
    };

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedAlerts()
    {
        var request = new AlertQueryRequest();
        var alerts = SamplePagedResponse(SampleResponse(1), SampleResponse(2));
        _service.Setup(s => s.GetAllAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(alerts);

        var result = await _controller.GetAll(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<PagedResponse<AlertResponse>>(ok.Value);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.TotalCount);
    }

    [Fact]
    public async Task GetAll_PassesQueryRequestToService()
    {
        var request = new AlertQueryRequest
        {
            IsActive = true,
            Severity = Severity.Critical,
            CreatedFrom = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedTo = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            Page = 2,
            PageSize = 10,
            SortBy = "title",
            SortDirection = "asc",
            Search = "disk"
        };
        _service.Setup(s => s.GetAllAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(SamplePagedResponse(SampleResponse(1)));

        _ = await _controller.GetAll(request, CancellationToken.None);

        _service.Verify(s => s.GetAllAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AlertQueryRequest_WithCreatedFromAfterCreatedTo_FailsValidation()
    {
        var request = new AlertQueryRequest
        {
            CreatedFrom = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            CreatedTo = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.CreatedFrom)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.CreatedTo)));
    }

    [Fact]
    public void AlertQueryRequest_WithInvalidValues_FailsValidation()
    {
        var request = new AlertQueryRequest
        {
            Page = 0,
            PageSize = 101,
            SortBy = "status",
            SortDirection = "sideways",
            Search = new string('x', 201)
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Page)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.PageSize)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.SortBy)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.SortDirection)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Search)));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateAlertRequest_WithWhitespaceOnlyTitle_FailsValidation(string title)
    {
        var request = new CreateAlertRequest
        {
            Title = title,
            Severity = Severity.High
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAlertRequest.Title)));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UpdateAlertRequest_WithWhitespaceOnlyTitle_FailsValidation(string title)
    {
        var request = new UpdateAlertRequest
        {
            Title = title,
            Severity = Severity.High
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAlertRequest.Title)));
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

        var result = await _controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, Assert.IsType<AlertResponse>(ok.Value).Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((AlertResponse?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetSummary_ReturnsOkWithTypedSummary()
    {
        var summary = new AlertSummaryResponse
        {
            TotalCount = 6,
            ActiveCount = 4,
            InactiveCount = 2,
            SeverityCounts = new AlertSeverityCountsResponse
            {
                Low = 1,
                Medium = 2,
                High = 1,
                Critical = 2
            }
        };
        _service.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertSummaryResponse>(ok.Value);
        Assert.Equal(6, body.TotalCount);
        Assert.Equal(2, body.SeverityCounts.Critical);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtRoute_WithLocationId()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse(5));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(nameof(AlertsController.GetById), created.RouteName);
        Assert.Equal(5, created.RouteValues!["id"]);
        Assert.Equal(5, Assert.IsType<AlertResponse>(created.Value).Id);
    }

    [Fact]
    public async Task Update_WhenFound_ReturnsOk()
    {
        var request = new UpdateAlertRequest { Title = "Updated", Severity = Severity.Low, IsActive = false };
        _service.Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

        var result = await _controller.Update(1, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var request = new UpdateAlertRequest { Title = "Updated", Severity = Severity.Low };
        _service.Setup(s => s.UpdateAsync(99, request, It.IsAny<CancellationToken>())).ReturnsAsync((AlertResponse?)null);

        var result = await _controller.Update(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Deactivate_WhenFound_ReturnsOk()
    {
        _service.Setup(s => s.DeactivateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Deactivate_WhenMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.DeactivateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((AlertResponse?)null);

        var result = await _controller.Deactivate(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WhenFound_ReturnsNoContent()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.DeleteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.Delete(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public async Task GetAll_PassesTagFilterToService()
    {
        var request = new AlertQueryRequest { Tag = "ops", IsActive = true, Severity = Severity.High, Search = "disk" };
        _service.Setup(s => s.GetAllAsync(It.IsAny<AlertQueryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SamplePagedResponse());

        _ = await _controller.GetAll(request, CancellationToken.None);

        _service.Verify(s => s.GetAllAsync(It.Is<AlertQueryRequest>(r => r.Tag == "ops" && r.IsActive == true && r.Severity == Severity.High && r.Search == "disk"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AlertQueryRequest_WithTagLongerThanMax_FailsValidation()
    {
        var results = Validate(new AlertQueryRequest { Tag = new string('x', AlertConstants.TagMaxLength + 1) });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }

    [Fact]
    public void AlertQueryRequest_WithTagAtMaxLengthOrNull_PassesValidation()
    {
        Assert.Empty(Validate(new AlertQueryRequest { Tag = new string('x', AlertConstants.TagMaxLength) }));
        Assert.Empty(Validate(new AlertQueryRequest { Tag = null }));
    }

    [Fact]
    public void AlertResponse_Tags_DefaultsToEmptyList()
    {
        Assert.Empty(new AlertResponse().Tags);
    }

    [Fact]
    public void AddAlertTagsRequest_WithValidTags_PassesValidation()
    {
        var request = new AddAlertTagsRequest { Tags = new() { "a", new string('x', AlertConstants.TagMaxLength), "  ops  " } };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void AddAlertTagsRequest_WithEmptyList_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new() });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AddAlertTagsRequest_WithEmptyOrWhitespaceTag_FailsValidation(string tag)
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new() { "ok", tag } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AddAlertTagsRequest_WithNullTag_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new() { null! } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AddAlertTagsRequest_WithTagLongerThanMax_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new() { new string('x', AlertConstants.TagMaxLength + 1) } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public async Task AddTags_WhenSuccess_ReturnsOkWithUpdatedAlert()
    {
        var request = new AddAlertTagsRequest { Tags = new() { "ops" } };
        var alert = SampleResponse(3);
        alert.Tags = new List<string> { "ops" };
        _service.Setup(s => s.AddTagsAsync(3, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.Success, alert));

        var result = await _controller.AddTags(3, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(3, body.Id);
        Assert.Equal(new[] { "ops" }, body.Tags);
    }

    [Fact]
    public async Task AddTags_WhenAlertMissing_ReturnsNotFound()
    {
        var request = new AddAlertTagsRequest { Tags = new() { "ops" } };
        _service.Setup(s => s.AddTagsAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.AlertNotFound));

        var result = await _controller.AddTags(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTags_WhenTooManyTags_ReturnsBadRequestWithTagsError()
    {
        var request = new AddAlertTagsRequest { Tags = new() { "ops" } };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.TooManyTags));

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.Contains(nameof(AddAlertTagsRequest.Tags), problem.Errors.Keys);
    }

    [Fact]
    public async Task RemoveTag_WhenRemoved_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.RemoveTag(1, "ops", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveTag_WhenAlertOrAssignmentMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(99, "ops", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.RemoveTag(99, "ops", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
