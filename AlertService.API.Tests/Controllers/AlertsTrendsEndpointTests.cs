using System.Net;
using System.Text.Json;
using AlertService.API.Tests.TestInfrastructure;

namespace AlertService.API.Tests.Controllers;

public class AlertsTrendsEndpointTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("91")]
    [InlineData("abc")]
    public async Task GetTrends_WithInvalidDays_ReturnsValidationProblemDetails400(string days)
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync($"/api/alerts/trends?days={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        var mediaType = response.Content.Headers.ContentType!.MediaType;
        Assert.True(
            string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase));

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("days", out var dayErrors));
        Assert.Equal(JsonValueKind.Array, dayErrors.ValueKind);
        Assert.NotEmpty(dayErrors.EnumerateArray());
    }
}
