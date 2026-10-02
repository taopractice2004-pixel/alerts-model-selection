using System.Net;
using System.Text.Json;
using AlertService.API.Tests.TestInfrastructure;

namespace AlertService.API.Tests;

[Collection(WebHostCollection.Name)]
public class HealthChecksTests
{
    [Fact]
    public async Task Live_ReturnsHealthyWithoutTouchingSql()
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: false);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await ReadPayloadAsync(response);
        AssertHealthPayload(payload.RootElement, "Healthy", [("live", "Healthy")]);
    }

    [Fact]
    public async Task Ready_ReturnsHealthyWhenDatabaseIsReachable()
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await ReadPayloadAsync(response);
        AssertHealthPayload(payload.RootElement, "Healthy", [("sql", "Healthy")]);
    }

    [Fact]
    public async Task Ready_ReturnsUnhealthyWhenDatabaseIsNotReachable()
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: false);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var payload = await ReadPayloadAsync(response);
        AssertHealthPayload(payload.RootElement, "Unhealthy", [("sql", "Unhealthy")]);
    }

    private static async Task<JsonDocument> ReadPayloadAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static void AssertHealthPayload(JsonElement root, string expectedStatus, IReadOnlyList<(string Name, string Status)> expectedChecks)
    {
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(["status", "duration", "checks"], root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("duration").ValueKind);

        var checks = root.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Equal(expectedChecks.Count, checks.Length);

        for (var index = 0; index < expectedChecks.Count; index++)
        {
            var check = checks[index];
            Assert.Equal(["name", "status", "duration"], check.EnumerateObject().Select(property => property.Name).ToArray());
            Assert.Equal(expectedChecks[index].Name, check.GetProperty("name").GetString());
            Assert.Equal(expectedChecks[index].Status, check.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Number, check.GetProperty("duration").ValueKind);
        }
    }
}