using System.Net;
using System.Text.Json;
using AlertService.API.Tests.TestInfrastructure;

namespace AlertService.API.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public class TrendsEndpointTests
{
    [Fact]
    public async Task GetTrends_WhenDaysOmitted_ReturnsDefaultSevenBuckets()
    {
        using var factory = new TrendsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        await factory.InitializeDatabaseAsync();

        using var response = await client.GetAsync("/api/alerts/trends");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var buckets = payload.RootElement.EnumerateArray().ToArray();
        Assert.Equal(7, buckets.Length);
        Assert.All(buckets, bucket =>
        {
            Assert.Equal(JsonValueKind.String, bucket.GetProperty("day").ValueKind);
            Assert.Equal(0, bucket.GetProperty("totalCount").GetInt32());

            var severityCounts = bucket.GetProperty("severityCounts");
            Assert.Equal(0, severityCounts.GetProperty("low").GetInt32());
            Assert.Equal(0, severityCounts.GetProperty("medium").GetInt32());
            Assert.Equal(0, severityCounts.GetProperty("high").GetInt32());
            Assert.Equal(0, severityCounts.GetProperty("critical").GetInt32());
        });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("91")]
    public async Task GetTrends_WithOutOfRangeDays_ReturnsValidationProblemDetails(string days)
    {
        using var factory = new TrendsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        await factory.InitializeDatabaseAsync();

        using var response = await client.GetAsync($"/api/alerts/trends?days={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", payload.RootElement.GetProperty("type").GetString());
        Assert.Equal(400, payload.RootElement.GetProperty("status").GetInt32());
        AssertValidationErrorForDays(payload.RootElement);
    }

    [Fact]
    public async Task GetTrends_WithNonNumericDays_ReturnsValidationProblemDetails()
    {
        using var factory = new TrendsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        await factory.InitializeDatabaseAsync();

        using var response = await client.GetAsync("/api/alerts/trends?days=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, payload.RootElement.GetProperty("status").GetInt32());
        AssertValidationErrorForDays(payload.RootElement);
    }

    private static void AssertValidationErrorForDays(JsonElement root)
    {
        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        var errors = root.GetProperty("errors");
        var matchingProperty = errors.EnumerateObject()
            .FirstOrDefault(property => string.Equals(property.Name, "Days", StringComparison.OrdinalIgnoreCase));

        Assert.False(string.IsNullOrEmpty(matchingProperty.Name));
        Assert.NotEmpty(matchingProperty.Value.EnumerateArray());
    }
}