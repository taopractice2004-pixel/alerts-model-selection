using System.Text.Json.Serialization;
using AlertService.API.Extensions;
using AlertService.API.Middleware;
using AlertService.API.Services;
using AlertService.Data.SQL.Extensions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before the host configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AlertService.API");

    var builder = WebApplication.CreateBuilder(args);

    // ---------- Logging ----------
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ---------- Services (Dependency Injection) ----------
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services
        .AddOptions<DuplicateSuppressionOptions>()
        .Bind(builder.Configuration.GetSection(DuplicateSuppressionOptions.SectionName))
        .Validate(options => options.WindowMinutes > 0, "Alert duplicate suppression window must be greater than zero.")
        .ValidateOnStart();

    builder.Services.AddSqlDataAccess(builder.Configuration);   // DbContext + repositories
    builder.Services.AddAlertHealthChecks();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<IAlertService, AlertManagementService>();

    var app = builder.Build();

    // ---------- HTTP pipeline ----------
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, exception) =>
        {
            if (httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            {
                return LogEventLevel.Debug;
            }

            return exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogEventLevel.Error
                : LogEventLevel.Information;
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        Log.Information("Applying database migrations");
        await app.Services.ApplyMigrationsAsync();
    }

    app.UseHttpsRedirection();
    app.MapAlertHealthEndpoints();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "AlertService.API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
