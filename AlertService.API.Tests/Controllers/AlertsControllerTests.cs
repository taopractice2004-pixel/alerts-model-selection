using System.ComponentModel.DataAnnotations;
using AlertService.API.Controllers;
using AlertService.API.Services;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Text.Json;

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
        Tags = ["infra", "ops"]
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
            Search = "disk",
            Tag = "infra"
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

    [Fact]
    public void AlertQueryRequest_WithWhitespaceTag_FailsValidation()
    {
        var request = new AlertQueryRequest
        {
            Tag = "   "
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }

    [Fact]
    public void AddTagsRequest_WithInvalidTagLength_FailsValidation()
    {
        var request = new AddTagsRequest
        {
            Tags = ["  ", new string('a', 31)]
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Tags[0]"));
        Assert.Contains(results, r => r.MemberNames.Contains("Tags[1]"));
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
    public async Task GetTrends_WithoutDays_UsesDefaultSevenDays()
    {
        var trends = new List<AlertDailyTrendResponse>
        {
            new()
            {
                DayUtc = new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc),
                TotalCount = 0,
                SeverityCounts = new AlertSeverityCountsResponse()
            }
        };
        _service.Setup(s => s.GetDailyTrendsAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(trends);

        var result = await _controller.GetTrends(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<AlertDailyTrendResponse>>(ok.Value);
        Assert.Single(body);
        _service.Verify(s => s.GetDailyTrendsAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrends_WithCustomDays_PassesDaysToService()
    {
        var trends = Array.Empty<AlertDailyTrendResponse>();
        _service.Setup(s => s.GetDailyTrendsAsync(14, It.IsAny<CancellationToken>())).ReturnsAsync(trends);

        var result = await _controller.GetTrends(14, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsAssignableFrom<IReadOnlyList<AlertDailyTrendResponse>>(ok.Value);
        _service.Verify(s => s.GetDailyTrendsAsync(14, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrends_WithOutOfRangeDays_ReturnsBadRequestValidationProblemDetails()
    {
        using var factory = new TrendsValidationWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/api/alerts/trends?days=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var payload = await ReadPayloadAsync(response);
        var root = payload.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        var errors = root.GetProperty("errors");
        Assert.True(ContainsErrorKey(errors, "days"));
        Assert.Equal(0, factory.ServiceSpy.DailyTrendCallCount);
    }

    [Fact]
    public async Task GetTrends_WithNonNumericDays_ReturnsBadRequestValidationProblemDetails()
    {
        using var factory = new TrendsValidationWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/api/alerts/trends?days=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var payload = await ReadPayloadAsync(response);
        var root = payload.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        var errors = root.GetProperty("errors");
        Assert.True(ContainsErrorKey(errors, "days"));
        Assert.Equal(0, factory.ServiceSpy.DailyTrendCallCount);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtRoute_WithLocationId()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service
            .Setup(s => s.CreateWithSuppressionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertCreateResult
            {
                Alert = SampleResponse(5),
                DuplicateSuppressed = false
            });

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(nameof(AlertsController.GetById), created.RouteName);
        Assert.Equal(5, created.RouteValues!["id"]);
        Assert.Equal(5, Assert.IsType<AlertResponse>(created.Value).Id);
        Assert.False(_controller.Response.Headers.ContainsKey("X-Duplicate-Suppressed"));
    }

    [Fact]
    public async Task Create_WhenDuplicateSuppressed_ReturnsOkWithSuppressionHeader()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service
            .Setup(s => s.CreateWithSuppressionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertCreateResult
            {
                Alert = SampleResponse(5),
                DuplicateSuppressed = true
            });

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(5, Assert.IsType<AlertResponse>(ok.Value).Id);
        Assert.True(_controller.Response.Headers.TryGetValue("X-Duplicate-Suppressed", out var value));
        Assert.Equal("true", value.ToString());
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
    public async Task AddTags_WhenAlertMissing_ReturnsNotFound()
    {
        var request = new AddTagsRequest { Tags = ["infra"] };
        _service.Setup(s => s.AddTagsAsync(10, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TagAssignmentResult.AlertNotFound());

        var result = await _controller.AddTags(10, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTags_WhenMaxExceeded_ReturnsBadRequestWithValidationProblem()
    {
        var request = new AddTagsRequest { Tags = ["infra", "ops"] };
        _service.Setup(s => s.AddTagsAsync(10, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TagAssignmentResult.MaxTagsExceeded());

        var result = await _controller.AddTags(10, request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(AddTagsRequest.Tags), problem.Errors.Keys);
    }

    [Fact]
    public async Task AddTags_WhenSuccessful_ReturnsUpdatedAlert()
    {
        var request = new AddTagsRequest { Tags = ["infra", "ops"] };
        var response = SampleResponse(10);
        _service.Setup(s => s.AddTagsAsync(10, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TagAssignmentResult.Success(response));

        var result = await _controller.AddTags(10, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(10, body.Id);
        Assert.Equal(2, body.Tags.Count);
    }

    [Fact]
    public async Task RemoveTag_WhenAssignmentExists_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(10, "infra", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.RemoveTag(10, "infra", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveTag_WhenAssignmentMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.RemoveTagAsync(10, "missing", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.RemoveTag(10, "missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static async Task<JsonDocument> ReadPayloadAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static bool ContainsErrorKey(JsonElement errors, string expectedFragment)
    {
        return errors.EnumerateObject().Any(property =>
            property.Name.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TrendsValidationWebApplicationFactory : WebApplicationFactory<Program>
    {
        public TrendsServiceSpy ServiceSpy { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrationsOnStartup"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAlertService>();
                services.AddSingleton<IAlertService>(ServiceSpy);
            });
        }
    }

    private sealed class TrendsServiceSpy : IAlertService
    {
        public int DailyTrendCallCount { get; private set; }

        public Task<PagedResponse<AlertResponse>> GetAllAsync(AlertQueryRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AlertResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AlertSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AlertDailyTrendResponse>> GetDailyTrendsAsync(int days, CancellationToken cancellationToken = default)
        {
            DailyTrendCallCount++;
            return Task.FromResult<IReadOnlyList<AlertDailyTrendResponse>>(Array.Empty<AlertDailyTrendResponse>());
        }

        public Task<AlertCreateResult> CreateWithSuppressionAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AlertResponse?> UpdateAsync(int id, UpdateAlertRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AlertResponse?> DeactivateAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TagAssignmentResult> AddTagsAsync(int id, AddTagsRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> RemoveTagAsync(int id, string tag, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
