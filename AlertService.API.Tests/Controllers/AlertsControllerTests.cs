using System.Net;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using AlertService.API.Controllers;
using AlertService.API.Services;
using AlertService.Data.SQL;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        Tags = ["Database", "Infra"]
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
    public void AddAlertTagsRequest_WithInvalidTags_FailsValidation()
    {
        var request = new AddAlertTagsRequest
        {
            Tags = ["valid", "   ", new string('x', 31)]
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
        Assert.Equal(2, results.Count);
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
    public async Task GetTrends_ReturnsOkWithTypedBuckets()
    {
        var request = new AlertTrendQueryRequest { Days = 3 };
        IReadOnlyList<AlertTrendBucketResponse> trends =
        [
            new()
            {
                Date = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                TotalCount = 2,
                SeverityCounts = new AlertSeverityCountsResponse
                {
                    Low = 1,
                    High = 1
                }
            }
        ];
        _service.Setup(s => s.GetTrendsAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(trends);

        var result = await _controller.GetTrends(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<AlertTrendBucketResponse>>(ok.Value);
        Assert.Single(body);
        Assert.Equal(2, body[0].TotalCount);
    }

    [Fact]
    public void AlertTrendQueryRequest_DefaultsDaysToSeven()
    {
        var request = new AlertTrendQueryRequest();

        Assert.Equal(AlertTrendQueryRequest.DefaultDays, request.Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void AlertTrendQueryRequest_WithOutOfRangeDays_FailsValidation(int days)
    {
        var request = new AlertTrendQueryRequest { Days = days };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(AlertTrendQueryRequest.Days)));
    }

    [Fact]
    public async Task GetTrends_WhenDaysOmitted_UsesDefaultDaysFromQueryBinding()
    {
        var capturedRequests = new List<AlertTrendQueryRequest>();
        var service = new Mock<IAlertService>(MockBehavior.Strict);
        service.Setup(s => s.GetTrendsAsync(It.IsAny<AlertTrendQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AlertTrendQueryRequest, CancellationToken>((request, _) => capturedRequests.Add(request))
            .ReturnsAsync(Array.Empty<AlertTrendBucketResponse>());

        using var factory = new TrendsWebApplicationFactory(services =>
        {
            services.RemoveAll<IAlertService>();
            services.AddScoped(_ => service.Object);
        });
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/api/alerts/trends");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(capturedRequests);
        Assert.Equal(AlertTrendQueryRequest.DefaultDays, capturedRequests[0].Days);
        service.Verify(s => s.GetTrendsAsync(It.IsAny<AlertTrendQueryRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("/api/alerts/trends?days=0")]
    [InlineData("/api/alerts/trends?days=91")]
    [InlineData("/api/alerts/trends?days=abc")]
    public async Task GetTrends_WithInvalidDays_ReturnsValidationProblemDetails(string url)
    {
        var service = new Mock<IAlertService>(MockBehavior.Strict);

        using var factory = new TrendsWebApplicationFactory(services =>
        {
            services.RemoveAll<IAlertService>();
            services.AddScoped(_ => service.Object);
        });
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, payload.RootElement.GetProperty("status").GetInt32());

        var errorKeys = payload.RootElement
            .GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains(errorKeys, key => string.Equals(key, nameof(AlertTrendQueryRequest.Days), StringComparison.OrdinalIgnoreCase));
        service.Verify(s => s.GetTrendsAsync(It.IsAny<AlertTrendQueryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtRoute_WithLocationId()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        _service.Setup(s => s.CreateWithSuppressionDetailsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SampleResponse(5), false));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(nameof(AlertsController.GetById), created.RouteName);
        Assert.Equal(5, created.RouteValues!["id"]);
        Assert.Equal(5, Assert.IsType<AlertResponse>(created.Value).Id);
        Assert.False(_controller.Response.Headers.ContainsKey("X-Duplicate-Suppressed"));
    }

    [Fact]
    public async Task Create_WhenDuplicateSuppressed_ReturnsOkWithHeader()
    {
        var request = new CreateAlertRequest { Title = "Disk usage high", Severity = Severity.High };
        var existingAlert = SampleResponse(8);
        _service.Setup(s => s.CreateWithSuppressionDetailsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((existingAlert, true));

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(existingAlert, ok.Value);
        Assert.Equal("true", _controller.Response.Headers["X-Duplicate-Suppressed"].ToString());
    }

    [Fact]
    public async Task AddTags_WhenSuccessful_ReturnsOkWithAlert()
    {
        var request = new AddAlertTagsRequest { Tags = ["database", "infra"] };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.Success,
                Alert = SampleResponse(1)
            });

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AlertResponse>(ok.Value);
        Assert.Equal(["Database", "Infra"], body.Tags);
    }

    [Fact]
    public async Task AddTags_WhenAlertMissing_ReturnsNotFound()
    {
        var request = new AddAlertTagsRequest { Tags = ["database"] };
        _service.Setup(s => s.AddTagsAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.AlertNotFound
            });

        var result = await _controller.AddTags(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddTags_WhenValidationFails_ReturnsBadRequestProblemDetails()
    {
        var request = new AddAlertTagsRequest { Tags = [] };
        _service.Setup(s => s.AddTagsAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.ValidationFailed,
                Errors = new Dictionary<string, string[]>
                {
                    [nameof(AddAlertTagsRequest.Tags)] = ["At least one tag is required."]
                }
            });

        var result = await _controller.AddTags(1, request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(["At least one tag is required."], details.Errors[nameof(AddAlertTagsRequest.Tags)]);
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
    public async Task RemoveTag_WhenSuccessful_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "Infra", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.Success
            });

        var result = await _controller.RemoveTag(1, "Infra", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Theory]
    [InlineData(AlertTagOperationStatus.AlertNotFound)]
    [InlineData(AlertTagOperationStatus.TagAssignmentNotFound)]
    public async Task RemoveTag_WhenTagOrAlertMissing_ReturnsNotFound(AlertTagOperationStatus status)
    {
        _service.Setup(s => s.RemoveTagAsync(1, "Infra", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = status
            });

        var result = await _controller.RemoveTag(1, "Infra", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveTag_WhenValidationFails_ReturnsBadRequestProblemDetails()
    {
        _service.Setup(s => s.RemoveTagAsync(1, "   ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertTagOperationResult
            {
                Status = AlertTagOperationStatus.ValidationFailed,
                Errors = new Dictionary<string, string[]>
                {
                    ["tag"] = ["Tag must be between 1 and 30 characters after trimming."]
                }
            });

        var result = await _controller.RemoveTag(1, "   ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(["Tag must be between 1 and 30 characters after trimming."], details.Errors["tag"]);
    }

    private sealed class TrendsWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection>? _configureServices;
        private SqliteConnection? _openConnection;

        public TrendsWebApplicationFactory(Action<IServiceCollection>? configureServices)
        {
            _configureServices = configureServices;
        }

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
                services.RemoveAll<DbContextOptions<AlertDbContext>>();
                services.RemoveAll<AlertDbContext>();

                _openConnection = new SqliteConnection("Data Source=:memory:");
                _openConnection.Open();
                services.AddDbContext<AlertDbContext>(options => options.UseSqlite(_openConnection));

                _configureServices?.Invoke(services);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _openConnection?.Dispose();
                _openConnection = null;
            }

            base.Dispose(disposing);
        }
    }
}
