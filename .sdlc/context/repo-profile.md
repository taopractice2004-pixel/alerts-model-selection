# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: AlertService alert-management microservice
- Primary stack: ASP.NET Core Web API on .NET 8, EF Core 8, SQL Server, Serilog, Swagger
- Repository shape: multi-project solution with API, shared libraries, data-access libraries, tests, SQL scripts, and SDLC automation assets
- Shared build/runtime settings: `net8.0`, nullable enabled, implicit usings enabled, local `dotnet-ef` tool pinned to `8.0.31`

## Architecture

- Layering: controller/API layer -> service layer -> data abstractions -> SQL/EF Core implementation -> SQL Server
- Entry points / composition roots: `AlertService.API/Program.cs` bootstraps DI, middleware, logging, Swagger, health checks, and optional migrations
- User-facing or external interfaces: REST endpoints under `/api/alerts`, health endpoints `/health/live` and `/health/ready`, generated Swagger in development
- Business logic locations: `AlertService.API/Services/AlertManagementService.cs` and related DTO/model mapping extensions
- Data / integration boundaries: `AlertService.Data` defines repository interfaces; `AlertService.Data.SQL` owns `AlertDbContext`, EF configurations, repositories, migrations, and SQL Server connectivity
- Runtime / infrastructure notes: Serilog logging, EF Core migrations can run on startup in development, readiness health checks depend on database connectivity

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile command: `dotnet build AlertService.sln`
- Test command: `dotnet test`
- Lint / format command: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core 8
- Environment/config notes: connection string comes from `AlertService.API/appsettings.json`; `Database:ApplyMigrationsOnStartup` in development can apply migrations automatically; newer SDKs may require `DOTNET_ROLL_FORWARD=Major`
- Operational assets: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`, `AlertService.API/AlertService.API.http`, log files under `AlertService.API/Logs/`

## Testing

- Main test layers: API controller/service tests, health-check integration tests, repository/data-access tests
- Test frameworks: xUnit, Moq, ASP.NET Core `WebApplicationFactory`, EF Core SQLite and InMemory test support, Coverlet collector

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment/runbook documentation: `NOT_AVAILABLE`
- Separate OpenAPI source document: `NOT_AVAILABLE` even though Swagger generation is configured

## Starter Notes

- Use README plus this file for repo-wide commands and architecture.
- Use `repository-map.md` to scope impacted areas before deeper source inspection.