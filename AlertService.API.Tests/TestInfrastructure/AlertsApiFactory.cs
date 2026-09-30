using AlertService.Data.SQL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlertService.API.Tests.TestInfrastructure;

/// <summary>In-memory SQLite-backed factory with a controllable clock, used to exercise the full HTTP pipeline.</summary>
public sealed class AlertsApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    public MutableTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["AlertSuppression:WindowMinutes"] = "15"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AlertDbContext>>();
            services.RemoveAll<AlertDbContext>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            services.AddDbContext<AlertDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }

    public async Task<HttpClient> CreateInitializedClientAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AlertDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        var client = CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }

        base.Dispose(disposing);
    }
}
