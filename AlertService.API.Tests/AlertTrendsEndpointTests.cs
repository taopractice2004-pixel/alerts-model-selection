using System.Net;
using System.Net.Http.Json;
using AlertService.API.Tests.TestInfrastructure;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Tests;

public class AlertTrendsEndpointTests
{
    [Fact]
    public async Task GetTrends_WhenDaysIsOmitted_ReturnsSevenUtcBuckets()
    {
        using var factory = new AlertTrendsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/api/alerts/trends");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<AlertTrendBucketResponse>>();

        Assert.NotNull(payload);
        Assert.Equal(7, payload.Count);
        Assert.Equal(new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc), payload[0].Date);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), payload[^1].Date);
        Assert.All(payload, bucket =>
        {
            Assert.Equal(0, bucket.TotalCount);
            Assert.Equal(0, bucket.SeverityCounts.Low);
            Assert.Equal(0, bucket.SeverityCounts.Medium);
            Assert.Equal(0, bucket.SeverityCounts.High);
            Assert.Equal(0, bucket.SeverityCounts.Critical);
        });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("91")]
    [InlineData("abc")]
    public async Task GetTrends_WithInvalidDays_ReturnsValidationProblemDetails(string days)
    {
        using var factory = new AlertTrendsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync($"/api/alerts/trends?days={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.NotNull(problem.Errors);
        Assert.Contains(problem.Errors.Keys, key => string.Equals(key, "Days", StringComparison.OrdinalIgnoreCase));
    }
}