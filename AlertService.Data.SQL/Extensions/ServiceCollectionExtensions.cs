using AlertService.Data.Interfaces;
using AlertService.Data.SQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AlertService.Data.SQL.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "AlertDb";

    /// <summary>
    /// Registers the SQL Server DbContext and repository implementations.
    /// </summary>
    public static IServiceCollection AddSqlDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AlertDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AlertDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddScoped<IAlertRepository, AlertRepository>();

        return services;
    }

    /// <summary>
    /// Registers liveness and readiness checks for the Alert service.
    /// </summary>
    public static IServiceCollection AddAlertHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("live", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<AlertDbContext>("sql", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations. Intended for local/dev environments only.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlertDbContext>();
        await context.Database.MigrateAsync();
    }
}
