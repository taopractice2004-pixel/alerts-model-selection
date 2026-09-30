# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: AlertService — an alert management microservice exposing a REST API over SQL Server.
- Primary stack: C# / .NET 8 (ASP.NET Core Web API), EF Core 8, SQL Server, Serilog, xUnit, Moq.
- Repository shape: Multi-project .NET solution (`AlertService.sln`) with layered projects
  (API, DTO, Common, Models, Data abstractions, Data.SQL, plus two test projects).
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, `ImplicitUsings=enable`,
  `Nullable=enable`, `TreatWarningsAsErrors=false` for every project.

## Architecture

- Layering: Controller -> Service -> Repository. Controllers are HTTP-only; business logic lives
  in the service layer; persistence is behind `IAlertRepository`.
- Entry points / composition roots: `AlertService.API/Program.cs` (DI, middleware, Serilog,
  Swagger, health checks, migrations bootstrap).
- User-facing or external interfaces: REST endpoints under `/api/alerts` in
  `AlertService.API/Controllers/AlertsController.cs`; health endpoints under `/health`.
- Business logic locations: `AlertService.API/Services/AlertManagementService.cs`
  (contract in `IAlertService.cs`); entity<->DTO mapping in `Mappings/AlertMappingExtensions.cs`.
- Data / integration boundaries: abstractions in `AlertService.Data` (`IAlertRepository`);
  SQL Server implementation in `AlertService.Data.SQL` (`AlertDbContext`, configurations,
  repositories, EF migrations). API references `Data.SQL` only for DI registration.
- Runtime / infrastructure notes: Serilog request logging, global `ExceptionHandlingMiddleware`
  returning ProblemDetails, optional migrate-on-startup via `Database:ApplyMigrationsOnStartup`.

## Build And Run

- Install / restore dependencies: `dotnet restore AlertService.sln`
  (EF tooling via `dotnet tool restore`, `dotnet-ef` 8.0.31 in `dotnet-tools.json`).
- Build / compile command: `dotnet build AlertService.sln`
- Test command: `dotnet test AlertService.sln`
- Lint / format command: `TO_BE_DISCOVERED` (no dedicated linter/formatter config found).
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core 8 (`AlertServiceDb`).
- Environment/config notes: `appsettings.json` / `appsettings.Development.json`; connection
  strings and `Database:ApplyMigrationsOnStartup` toggle drive persistence/migrations.
- Operational assets: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`
  (idempotent script generated from EF migrations); Serilog file/console sinks (`AlertService.API/Logs`).

## Testing

- Main test layers: API controller + service unit tests (`AlertService.API.Tests`);
  repository tests (`AlertService.Data.SQL.Tests`); health-check integration tests via
  `WebApplicationFactory<Program>`.
- Test frameworks: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, EF Core Sqlite/InMemory,
  coverlet for coverage.

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE` until discovered
- Deployment/runbook documentation: `NOT_AVAILABLE` until discovered
- Separate OpenAPI contract artifact in source control: `NOT_AVAILABLE` (Swagger generated at
  runtime via Swashbuckle; no checked-in spec found)
- Lint/format tooling: `TO_BE_DISCOVERED`

## Starter Notes

- Cache produced by `/setup-repo-context` from the current repository state.
- `/analyze-story` and `/implement-story` should rely on this file before rereading source.