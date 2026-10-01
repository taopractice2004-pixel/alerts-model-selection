# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: `AlertService`, an alert-management microservice exposing a REST API
- Primary stack: `C#`, `.NET 8`, `ASP.NET Core Web API`, `Entity Framework Core 8`, `SQL Server`, `Serilog`, `xUnit`
- Repository shape: multi-project solution with API, DTO, domain model, data abstraction, SQL implementation, and dedicated test projects
- Shared build/runtime settings: `Directory.Build.props` targets `net8.0`, enables nullable reference types and implicit usings, and does not treat warnings as errors

## Architecture

- Layering: controller/API surface -> service layer -> repository abstraction -> EF Core SQL implementation -> SQL Server
- Entry points / composition roots: `AlertService.API\Program.cs` bootstraps DI, logging, Swagger, health checks, middleware, and optional migration application
- User-facing or external interfaces: REST endpoints in `AlertService.API\Controllers\AlertsController.cs`, health endpoints in `AlertService.API\Extensions\HealthCheckEndpointExtensions.cs`, runtime Swagger UI in Development
- Business logic locations: `AlertService.API\Services\AlertManagementService.cs` plus DTO/entity mapping extensions in `AlertService.API\Mappings\`
- Data / integration boundaries: `AlertService.Data\Interfaces\IAlertRepository.cs`, `AlertService.Data.SQL\AlertRepository.cs`, `AlertService.Data.SQL\AlertDbContext.cs`, SQL scripts under `database\`
- Runtime / infrastructure notes: Serilog writes to console and rolling files, global exception middleware returns RFC7807-style `ProblemDetails`, optional `Database:ApplyMigrationsOnStartup` applies EF migrations during startup

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile command: `dotnet build AlertService.sln`
- Test command: `dotnet test`
- Lint / format command: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server / LocalDB via EF Core (`AlertDb` connection string)
- Environment/config notes: connection strings and Serilog live in `AlertService.API\appsettings.json`; `appsettings.Development.json` enables `Database:ApplyMigrationsOnStartup`; newer SDK-only environments may need `DOTNET_ROLL_FORWARD=Major`
- Operational assets: health probes at `/health/live` and `/health/ready`, EF migrations in `AlertService.Data.SQL\Migrations\`, SQL bootstrap scripts in `database\`, rolling logs in `AlertService.API\Logs\`

## Testing

- Main test layers: API controller/service tests plus health endpoint integration-style tests in `AlertService.API.Tests\`; repository/data-access tests in `AlertService.Data.SQL.Tests\`
- Test frameworks: `xUnit`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `EF Core InMemory`, `SQLite`

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment/runbook documentation: `NOT_AVAILABLE`
- Separate API contract artifacts: `NOT_AVAILABLE` (Swagger is generated at runtime; no standalone OpenAPI file is checked in)