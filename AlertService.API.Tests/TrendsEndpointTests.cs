using System.Net;
using System.Text.Json;
using AlertService.API.Services;
using AlertService.API.Tests.TestInfrastructure;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AlertService.API.Tests;

// Web-host tests with a mocked IAlertService: they prove query binding, validation, and JSON shape, not the data path.
[Collection(WebHostCollection.Name)]
public class TrendsEndpointTests
{
    private readonly Mock<IAlertService> _service = new();

    private HttpClient CreateClient(HealthChecksWebApplicationFactory factory)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAlertService>();
                services.AddSingleton(_service.Object);
            }))
            .CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    [Theory]
    [InlineData("days=0")]
    [InlineData("days=-1")]
    [InlineData("days=91")]
    [InlineData("days=abc")]
    public async Task GetTrends_InvalidDays_ReturnsValidationProblemDetails(string query)
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/api/alerts/trends?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, payload.RootElement.GetProperty("status").GetInt32());
        Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("Days", out _));
        _service.Verify(s => s.GetTrendsAsync(It.IsAny<AlertTrendsQueryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTrends_WithoutDays_UsesDefaultSevenAndReturnsBuckets()
    {
        AlertTrendsQueryRequest? captured = null;
        _service.Setup(s => s.GetTrendsAsync(It.IsAny<AlertTrendsQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AlertTrendsQueryRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new AlertTrendsResponse
            {
                Days = 7,
                Buckets =
                [
                    new AlertTrendBucketResponse
                    {
                        Date = new DateOnly(2026, 9, 1),
                        TotalCount = 3,
                        SeverityCounts = new AlertSeverityCountsResponse { Low = 1, High = 1, Critical = 1 }
                    }
                ]
            });
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/api/alerts/trends");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(7, captured!.Days);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, payload.RootElement.GetProperty("days").GetInt32());
        var bucket = payload.RootElement.GetProperty("buckets")[0];
        Assert.Equal("2026-09-01", bucket.GetProperty("date").GetString());
        Assert.Equal(3, bucket.GetProperty("totalCount").GetInt32());
        var severityCounts = bucket.GetProperty("severityCounts");
        Assert.Equal(["low", "medium", "high", "critical"], severityCounts.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    public async Task GetTrends_BoundaryDays_AreBoundToRequest(int days)
    {
        AlertTrendsQueryRequest? captured = null;
        _service.Setup(s => s.GetTrendsAsync(It.IsAny<AlertTrendsQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AlertTrendsQueryRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new AlertTrendsResponse { Days = days });
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/api/alerts/trends?days={days}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(days, captured!.Days);
    }
}
