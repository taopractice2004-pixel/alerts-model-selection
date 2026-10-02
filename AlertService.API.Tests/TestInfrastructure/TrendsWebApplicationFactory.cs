using AlertService.Data.SQL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlertService.API.Tests.TestInfrastructure;

public sealed class TrendsWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _openConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AlertDbContext>>();
            services.RemoveAll<AlertDbContext>();

            _openConnection = new SqliteConnection("Data Source=:memory:");
            _openConnection.Open();

            services.AddDbContext<AlertDbContext>(options => options.UseSqlite(_openConnection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlertDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _openConnection?.Dispose();
            _openConnection = null;
        }

        base.Dispose(disposing);
    }
}