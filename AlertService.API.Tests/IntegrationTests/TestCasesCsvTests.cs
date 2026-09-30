using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlertService.API.Tests.TestInfrastructure;
using AlertService.Common.Enums;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;

namespace AlertService.API.Tests.IntegrationTests;

/// <summary>
/// Executable integration coverage for the manual test cases recorded in testcases.csv
/// (stories ALERT-410 tagging, ALERT-411 duplicate suppression, ALERT-412 trends).
/// Test method names are prefixed with the TestCaseID they automate.
/// </summary>
public class TestCasesCsvTests : IAsyncLifetime
{
    // The API serializes enums as strings; the default response deserialization needs the matching converter.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private AlertsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new AlertsApiFactory();
        _client = await _factory.CreateInitializedClientAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<AlertResponse> CreateAlertAsync(
        string title = "Disk usage high",
        Severity severity = Severity.High,
        string? description = "d",
        bool isActive = true)
    {
        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = title,
            Severity = severity,
            Description = description,
            IsActive = isActive
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> AddTagsAsync(int alertId, params string[] tags) =>
        _client.PostAsJsonAsync($"/api/alerts/{alertId}/tags", new AddTagsRequest { Tags = [.. tags] });

    // ---------- ALERT-410: tagging ----------

    [Fact]
    public async Task TC_410_01_AddSingleTag_ReturnsOkWithTag()
    {
        var alert = await CreateAlertAsync();

        var response = await AddTagsAsync(alert.Id, "ops");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.Contains("ops", body!.Tags);
    }

    [Fact]
    public async Task TC_410_02_AddMultipleTags_ReturnsBothTags()
    {
        var alert = await CreateAlertAsync();

        var response = await AddTagsAsync(alert.Id, "ops", "urgent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.Contains("ops", body!.Tags);
        Assert.Contains("urgent", body.Tags);
    }

    [Fact]
    public async Task TC_410_03_CaseInsensitiveDuplicateTag_IsNotDuplicated()
    {
        var alert = await CreateAlertAsync();
        await AddTagsAsync(alert.Id, "ops");

        var response = await AddTagsAsync(alert.Id, "Ops");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.Single(body!.Tags, t => string.Equals(t, "ops", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_410_04_ExceedingMaxTags_ReturnsBadRequest()
    {
        var alert = await CreateAlertAsync();
        await AddTagsAsync(alert.Id, [.. Enumerable.Range(1, 10).Select(i => $"tag{i}")]);

        var response = await AddTagsAsync(alert.Id, "extra");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_410_05_OneCharacterTag_IsAccepted()
    {
        var alert = await CreateAlertAsync();

        var response = await AddTagsAsync(alert.Id, "a");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_410_06_ThirtyOneCharacterTag_IsRejected()
    {
        var alert = await CreateAlertAsync();

        var response = await AddTagsAsync(alert.Id, new string('a', 31));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_410_07_AddTagToNonExistentAlert_ReturnsNotFound()
    {
        var response = await AddTagsAsync(999999, "ops");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_410_08_DeleteExistingTag_ReturnsNoContentAndRemovesTag()
    {
        var alert = await CreateAlertAsync();
        await AddTagsAsync(alert.Id, "ops");

        var response = await _client.DeleteAsync($"/api/alerts/{alert.Id}/tags/ops");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await _client.GetFromJsonAsync<AlertResponse>($"/api/alerts/{alert.Id}", JsonOptions);
        Assert.DoesNotContain("ops", getResponse!.Tags);
    }

    [Fact]
    public async Task TC_410_09_DeleteMissingTag_ReturnsNotFound()
    {
        var alert = await CreateAlertAsync();

        var response = await _client.DeleteAsync($"/api/alerts/{alert.Id}/tags/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_410_10_FilterByTag_ReturnsOnlyTaggedAlerts()
    {
        var tagged = await CreateAlertAsync(title: "Tagged alert");
        var untagged = await CreateAlertAsync(title: "Untagged alert", severity: Severity.Low);
        await AddTagsAsync(tagged.Id, "ops");

        var response = await _client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts?tag=ops", JsonOptions);

        Assert.All(response!.Items, item => Assert.Contains("ops", item.Tags));
        Assert.Contains(response.Items, item => item.Id == tagged.Id);
        Assert.DoesNotContain(response.Items, item => item.Id == untagged.Id);
    }

    [Fact]
    public async Task TC_410_11_TagFilterComposesWithSeverityAndActiveFilters()
    {
        var match = await CreateAlertAsync(title: "Match alert", severity: Severity.High);
        await AddTagsAsync(match.Id, "ops");

        var wrongSeverity = await CreateAlertAsync(title: "Wrong severity", severity: Severity.Low);
        await AddTagsAsync(wrongSeverity.Id, "ops");

        var inactiveMatch = await CreateAlertAsync(title: "Inactive match", severity: Severity.High);
        await AddTagsAsync(inactiveMatch.Id, "ops");
        await _client.PatchAsync($"/api/alerts/{inactiveMatch.Id}/deactivate", null);

        var response = await _client.GetFromJsonAsync<PagedResponse<AlertResponse>>(
            "/api/alerts?tag=ops&severity=High&isActive=true", JsonOptions);

        Assert.Single(response!.Items);
        Assert.Equal(match.Id, response.Items[0].Id);
    }

    // ---------- ALERT-411: duplicate suppression ----------

    [Fact]
    public async Task TC_411_01_UniqueAlert_CreatesNormally()
    {
        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = "Disk usage high",
            Description = "d",
            Severity = Severity.High
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Duplicate-Suppressed"));
    }

    [Fact]
    public async Task TC_411_02_ImmediateDuplicate_IsSuppressed()
    {
        var first = await CreateAlertAsync(title: "Disk usage high", severity: Severity.High);

        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = "disk usage high",
            Description = "d2",
            Severity = Severity.High
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Duplicate-Suppressed"));
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.Equal(first.Id, body!.Id);
    }

    [Fact]
    public async Task TC_411_03_DifferentSeverity_IsNotSuppressed()
    {
        var first = await CreateAlertAsync(title: "Disk usage high", severity: Severity.High);

        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = "Disk usage high",
            Description = "d",
            Severity = Severity.Critical
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.NotEqual(first.Id, body!.Id);
    }

    [Fact]
    public async Task TC_411_04_DuplicateOfInactiveAlert_IsNotSuppressed()
    {
        var first = await CreateAlertAsync(title: "Disk usage high", severity: Severity.High);
        await _client.PatchAsync($"/api/alerts/{first.Id}/deactivate", null);

        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = "Disk usage high",
            Description = "d",
            Severity = Severity.High
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TC_411_05_DuplicateAfterWindowElapsed_IsNotSuppressed()
    {
        await CreateAlertAsync(title: "Disk usage high", severity: Severity.High);
        _factory.TimeProvider.Advance(TimeSpan.FromMinutes(16)); // configured window is 15 minutes

        var response = await _client.PostAsJsonAsync("/api/alerts", new CreateAlertRequest
        {
            Title = "Disk usage high",
            Description = "d",
            Severity = Severity.High
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public void TC_411_06_SuppressionWindow_IsConfigurableNotHardcoded()
    {
        var appSettingsPath = Path.Combine(FindRepoRoot(), "AlertService.API", "appsettings.json");
        var json = File.ReadAllText(appSettingsPath);

        Assert.Contains("WindowMinutes", json);
    }

    // ---------- ALERT-412: trends ----------

    [Fact]
    public async Task TC_412_01_DefaultTrendsCall_Returns7Buckets()
    {
        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends", JsonOptions);

        Assert.Equal(7, response!.Days.Count);
    }

    [Fact]
    public async Task TC_412_02_ExplicitDaysEquals1_Returns1Bucket()
    {
        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends?days=1", JsonOptions);

        Assert.Single(response!.Days);
    }

    [Fact]
    public async Task TC_412_03_MaxBoundaryDays90_IsAccepted()
    {
        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends?days=90", JsonOptions);

        Assert.Equal(90, response!.Days.Count);
    }

    [Fact]
    public async Task TC_412_04_OverMaxDays91_IsRejected()
    {
        var response = await _client.GetAsync("/api/alerts/trends?days=91");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // ASP.NET Core's automatic model-validation response body is shaped like ValidationProblemDetails
        // (title/status/errors), even though the default content-type is application/json, not +problem.
        Assert.Contains("\"errors\"", (await response.Content.ReadAsStringAsync()).ToLowerInvariant());
    }

    [Fact]
    public async Task TC_412_05_ZeroDays_IsRejected()
    {
        var response = await _client.GetAsync("/api/alerts/trends?days=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_412_06_NegativeDays_IsRejected()
    {
        var response = await _client.GetAsync("/api/alerts/trends?days=-5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_412_07_NonNumericDays_IsRejected()
    {
        var response = await _client.GetAsync("/api/alerts/trends?days=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_412_08_ZeroCountDay_StillAppearsAsBucket()
    {
        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends?days=7", JsonOptions);

        Assert.Equal(7, response!.Days.Count);
        Assert.All(response.Days, bucket =>
        {
            Assert.Equal(0, bucket.TotalCount);
            Assert.Equal(0, bucket.SeverityCounts.Low);
            Assert.Equal(0, bucket.SeverityCounts.Medium);
            Assert.Equal(0, bucket.SeverityCounts.High);
            Assert.Equal(0, bucket.SeverityCounts.Critical);
        });
    }

    [Fact]
    public async Task TC_412_09_BucketsAreOrderedOldestFirst()
    {
        var today = new DateTimeOffset(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);

        _factory.TimeProvider.SetUtcNow(today.AddDays(-6));
        await CreateAlertAsync(title: "Oldest alert");

        _factory.TimeProvider.SetUtcNow(today);
        await CreateAlertAsync(title: "Newest alert");

        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends?days=7", JsonOptions);

        var oldestExpected = DateOnly.FromDateTime(today.UtcDateTime).AddDays(-6);
        var newestExpected = DateOnly.FromDateTime(today.UtcDateTime);

        Assert.Equal(oldestExpected, response!.Days[0].Date);
        Assert.Equal(newestExpected, response.Days[^1].Date);
        Assert.Equal(1, response.Days[0].TotalCount);
        Assert.Equal(1, response.Days[^1].TotalCount);
    }

    [Fact]
    public async Task TC_412_10_PerDaySeverityCountsSumToTotal()
    {
        await CreateAlertAsync(title: "High severity today", severity: Severity.High);
        await CreateAlertAsync(title: "Critical severity today", severity: Severity.Critical);

        var response = await _client.GetFromJsonAsync<AlertTrendResponse>("/api/alerts/trends?days=1", JsonOptions);

        var bucket = Assert.Single(response!.Days);
        var sum = bucket.SeverityCounts.Low + bucket.SeverityCounts.Medium + bucket.SeverityCounts.High + bucket.SeverityCounts.Critical;
        Assert.Equal(bucket.TotalCount, sum);
        Assert.Equal(2, bucket.TotalCount);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AlertService.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repository root (AlertService.sln) from test output directory.");
    }
}
