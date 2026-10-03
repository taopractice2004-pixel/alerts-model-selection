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
        _controller = new AlertsController(_service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
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
    public async Task GetTrends_ReturnsOk_WithServiceBuckets()
    {
        var request = new AlertTrendsQueryRequest { Days = 2 };
        IReadOnlyList<AlertTrendBucketResponse> buckets =
        [
            new() { Date = new DateOnly(2026, 8, 31), TotalCount = 0, SeverityCounts = new AlertSeverityCountsResponse() },
            new() { Date = new DateOnly(2026, 9, 1), TotalCount = 3, SeverityCounts = new AlertSeverityCountsResponse { High = 3 } }
        ];
        _service.Setup(s => s.GetTrendsAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(buckets);

        var result = await _controller.GetTrends(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<AlertTrendBucketResponse>>(ok.Value);
        Assert.Equal(2, body.Count);
        Assert.Equal(new DateOnly(2026, 9, 1), body[1].Date);
        Assert.Equal(3, body[1].SeverityCounts.High);
        _service.Verify(s => s.GetTrendsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrends_PassesCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        var request = new AlertTrendsQueryRequest();
        _service.Setup(s => s.GetTrendsAsync(request, cts.Token)).ReturnsAsync(new List<AlertTrendBucketResponse>());

        await _controller.GetTrends(request, cts.Token);

        _service.Verify(s => s.GetTrendsAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtRoute_WithLocationId()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new CreateAlertResult(SampleResponse(5), false));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(nameof(AlertsController.GetById), created.RouteName);
        Assert.Equal(5, created.RouteValues!["id"]);
        Assert.Equal(5, Assert.IsType<AlertResponse>(created.Value).Id);
        Assert.False(_controller.Response.Headers.ContainsKey(AlertConstants.DuplicateSuppressedHeader));
    }

    [Fact]
    public async Task Create_WhenDuplicateSuppressed_ReturnsOk_WithHeader()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new CreateAlertResult(SampleResponse(3), true));

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsType<AlertResponse>(ok.Value).Id);
        Assert.Equal("true", _controller.Response.Headers[AlertConstants.DuplicateSuppressedHeader].ToString());
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

    [Fact]
    public async Task AddTags_WhenAdded_ReturnsOkWithUpdatedAlert()
    {
        var request = new AddAlertTagsRequest { Tags = new List<string> { "urgent", "network" } };
        var updated = SampleResponse();
        updated.Tags = new[] { "network", "urgent" };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.Added, updated));

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(new[] { "network", "urgent" }, body.Tags);
    }

    [Fact]
    public async Task AddTags_WhenAlertMissing_ReturnsNotFound()
    {
        var request = new AddAlertTagsRequest { Tags = new List<string> { "urgent" } };
        _service.Setup(s => s.AddTagsAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.AlertNotFound, null));

        var result = await _controller.AddTags(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTags_WhenLimitExceeded_ReturnsBadRequestWithTagsError()
    {
        var request = new AddAlertTagsRequest { Tags = new List<string> { "urgent" } };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddAlertTagsResult(AddAlertTagsStatus.TagLimitExceeded, null));
        var problemFactory = new Mock<ProblemDetailsFactory>();
        problemFactory.Setup(f => f.CreateValidationProblemDetails(
                It.IsAny<HttpContext>(),
                It.IsAny<ModelStateDictionary>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns((HttpContext? _, ModelStateDictionary modelState, int? status, string? _, string? _, string? _, string? _) =>
                new ValidationProblemDetails(modelState) { Status = status ?? StatusCodes.Status400BadRequest });
        _controller.ProblemDetailsFactory = problemFactory.Object;

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.ContainsKey(nameof(AddAlertTagsRequest.Tags)));
        Assert.True(_controller.ModelState.ContainsKey(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public async Task RemoveTag_WhenRemoved_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "urgent", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.RemoveTag(1, "urgent", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveTag_WhenAlertOrTagMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "missing", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.RemoveTag(1, "missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
