using System.ComponentModel.DataAnnotations;
using AlertService.API.Controllers;
using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertService.API.Tests.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _service = new();
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        _controller = new AlertsController(_service.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
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
        IsActive = true,
        Tags = []
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
    public async Task GetTrends_ReturnsOkWithTypedResponse()
    {
        var trends = new AlertTrendsResponse
        {
            Days = 3,
            Buckets =
            [
                new AlertTrendBucketResponse
                {
                    Date = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                    TotalCount = 1,
                    SeverityCounts = new AlertSeverityCountsResponse { Low = 1, Medium = 0, High = 0, Critical = 0 }
                },
                new AlertTrendBucketResponse
                {
                    Date = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                    TotalCount = 0,
                    SeverityCounts = new AlertSeverityCountsResponse()
                },
                new AlertTrendBucketResponse
                {
                    Date = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    TotalCount = 2,
                    SeverityCounts = new AlertSeverityCountsResponse { Low = 0, Medium = 1, High = 1, Critical = 0 }
                }
            ]
        };
        var request = new AlertTrendsQueryRequest { Days = 3 };
        _service.Setup(s => s.GetTrendsAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(trends);

        var result = await _controller.GetTrends(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertTrendsResponse>(ok.Value);
        Assert.Equal(3, body.Days);
        Assert.Equal(3, body.Buckets.Count);
        Assert.Equal(0, body.Buckets[1].TotalCount);
        Assert.Equal(1, body.Buckets[2].SeverityCounts.Medium);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void AlertTrendsQueryRequest_WithOutOfRangeDays_FailsValidation(int days)
    {
        var request = new AlertTrendsQueryRequest { Days = days };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertTrendsQueryRequest.Days)));
    }

    [Fact]
    public void AlertTrendsQueryRequest_DefaultDays_IsSeven()
    {
        var request = new AlertTrendsQueryRequest();

        Assert.Equal(AlertConstants.DefaultTrendDays, request.Days);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtRoute_WithLocationId()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertCreateResult
            {
                Alert = SampleResponse(5),
                IsDuplicateSuppressed = false
            });

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(nameof(AlertsController.GetById), created.RouteName);
        Assert.Equal(5, created.RouteValues!["id"]);
        Assert.Equal(5, Assert.IsType<AlertResponse>(created.Value).Id);
    }

    [Fact]
    public async Task Create_WhenDuplicateSuppressed_ReturnsOk_WithSuppressionHeader()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertCreateResult
            {
                Alert = SampleResponse(7),
                IsDuplicateSuppressed = true
            });

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("true", _controller.Response.Headers["X-Duplicate-Suppressed"].ToString());
        Assert.Equal(7, Assert.IsType<AlertResponse>(ok.Value).Id);
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
    public async Task AddTag_WhenSuccess_ReturnsOkWithAlert()
    {
        var request = new AddAlertTagRequest { Tag = "Ops" };
        _service.Setup(s => s.AddTagAsync(1, "Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.Success,
                Alert = SampleResponse(1)
            });

        var result = await _controller.AddTag(1, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, Assert.IsType<AlertResponse>(ok.Value).Id);
    }

    [Fact]
    public async Task AddTag_WhenAlertMissing_ReturnsNotFound()
    {
        var request = new AddAlertTagRequest { Tag = "Ops" };
        _service.Setup(s => s.AddTagAsync(99, "Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult { Status = AlertTagOperationStatus.AlertNotFound });

        var result = await _controller.AddTag(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTag_WhenMaxReached_ReturnsValidationProblem()
    {
        var request = new AddAlertTagRequest { Tag = "Ops" };
        _service.Setup(s => s.AddTagAsync(1, "Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult { Status = AlertTagOperationStatus.MaxTagsReached });

        var result = await _controller.AddTag(1, request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.IsType<ValidationProblemDetails>(objectResult.Value);
    }

    [Fact]
    public async Task RemoveTag_WhenSuccess_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult { Status = AlertTagOperationStatus.Success });

        var result = await _controller.RemoveTag(1, "ops", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveTag_WhenTagNotAssigned_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult { Status = AlertTagOperationStatus.TagNotAssigned });

        var result = await _controller.RemoveTag(1, "ops", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
