using System.Net;
using System.Net.Http.Json;
using AlertService.API.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Tests;

public class AlertTrendsEndpointTests
{
    [Theory]
    [InlineData("/api/alerts/trends?days=0")]
    [InlineData("/api/alerts/trends?days=91")]
    [InlineData("/api/alerts/trends?days=abc")]
    public async Task GetTrends_WithInvalidDays_ReturnsValidationProblem(string path)
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: false);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem!.Errors, entry => string.Equals(entry.Key, "days", StringComparison.OrdinalIgnoreCase));
    }
}