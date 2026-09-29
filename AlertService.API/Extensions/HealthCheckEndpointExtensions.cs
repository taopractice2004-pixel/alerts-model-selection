using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AlertService.API.Extensions;

public static class HealthCheckEndpointExtensions
{
    public static IEndpointRouteBuilder MapAlertHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", CreateOptions("live"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        endpoints.MapHealthChecks("/health/ready", CreateOptions("ready"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(string tag) => new()
    {
        Predicate = registration => registration.Tags.Contains(tag),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        },
        ResponseWriter = WriteResponseAsync
    };

    private static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        if (context.Request.Path.StartsWithSegments("/health/ready", StringComparison.OrdinalIgnoreCase) &&
            report.Status != HealthStatus.Healthy)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AlertService.API.HealthChecks");

            foreach (var (name, entry) in report.Entries.Where(pair => pair.Value.Status != HealthStatus.Healthy))
            {
                logger.LogWarning(entry.Exception, "Readiness health check {HealthCheckName} reported {HealthStatus}", name, entry.Status);
            }
        }

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}