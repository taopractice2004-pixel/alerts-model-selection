# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`/setup-repo-context 1`)

## Overview

- Product: AlertService — alert management microservice (create, list/filter/page/sort, summary, update, deactivate, delete alerts)
- Primary stack: ASP.NET Core Web API on .NET 8, EF Core 8.0.31 + SQL Server, Serilog, Swashbuckle
- Repository shape: single .NET solution (`AlertService.sln`) with layered class libraries, one API host, two test projects
- Shared build/runtime settings: `Directory.Build.props` → `net8.0`, `ImplicitUsings` enabled, `Nullable` enabled, `TreatWarningsAsErrors=false`; local tool `dotnet-ef 8.0.31` (`dotnet-tools.json`)

## Requirements Summary

Not applicable

## Architecture

- Layering: Controller (HTTP only) → Service (`IAlertService`/`AlertManagementService`, business logic) → Repository (`IAlertRepository`) → EF Core `AlertDbContext`
- Entry points / composition roots: `AlertService.API/Program.cs` (DI, Serilog, Swagger in Development, optional migrations on startup via `Database:ApplyMigrationsOnStartup`, `ExceptionHandlingMiddleware`)
- User-facing or external interfaces: REST `/api/alerts` (POST, GET paged, GET `/summary`, GET/PUT/DELETE `/{id}`, PATCH `/{id}/deactivate`); health `/health/live`, `/health/ready`
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs`; entity↔DTO mapping in `AlertService.API/Mappings/AlertMappingExtensions.cs`
- Data / integration boundaries: `AlertService.Data` (abstractions) vs `AlertService.Data.SQL` (SQL Server impl, registered via `AddSqlDataAccess`)
- Notable dependency flow: API → DTO → Common; API → Data (interfaces) → Models → Common; API → Data.SQL → Data. Enums serialized as strings (`JsonStringEnumConverter`). Unhandled errors → 500 ProblemDetails.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer, services, composition root | `Controllers/`, `Services/`, `Mappings/`, `Middleware/`, `Extensions/` (health checks), `Program.cs`, `appsettings*.json`, `AlertService.API.http` |
| `AlertService.DTO/` | Request/response contracts | `Requests/` (`AlertQueryRequest`, `CreateAlertRequest`, `UpdateAlertRequest`), `Responses/` (`AlertResponse`, `AlertSummaryResponse`, `AlertSeverityCountsResponse`, `PagedResponse`) |
| `AlertService.Models/` | Domain entity | `Alert.cs` |
| `AlertService.Common/` | Shared enums/constants | `Enums/Severity.cs`, `Constants/AlertConstants.cs` |
| `AlertService.Data/` | Data-access abstractions | `Interfaces/IAlertRepository.cs` |
| `AlertService.Data.SQL/` | SQL Server persistence | `AlertDbContext.cs`, `Configurations/`, `Repositories/AlertRepository.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Migrations/` (generated) |
| `database/` | SQL scripts | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` (idempotent, generated from EF migrations) |
| `AlertService.API.Tests/` | Controller, service, health-check tests | xUnit + Moq; `TestInfrastructure/HealthChecksWebApplicationFactory.cs` |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit + EF Core InMemory/SQLite |
| `standards/`, `.github/instructions/standards/` | Engineering standards (full / compact) | See `standards-summary.md` |

## Build And Run

- Install / restore dependencies: `dotnet restore AlertService.sln`; `dotnet tool restore` (dotnet-ef)
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test AlertService.sln`
- Coverage: `dotnet test AlertService.sln --collect:"XPlat Code Coverage"` (`coverlet.collector` referenced only in `AlertService.API.Tests`, not in `AlertService.Data.SQL.Tests`)
- Lint / format: `NOT_CONFIGURED` (no `.editorconfig` or analyzer config found)
- Run main app locally: `dotnet run --project AlertService.API` (see `launchSettings.json` for URLs)

## Data And Operations

- Primary data store: SQL Server (`ConnectionStrings:AlertDb`, default LocalDB `AlertServiceDb`); EF Core migrations in `AlertService.Data.SQL/Migrations`; scripts in `database/`
- Environment / config notes: `appsettings.json` / `appsettings.Development.json`; `Database:ApplyMigrationsOnStartup` (default `false`); Serilog console + rolling file `Logs/alertservice-.log`
- Operational assets: health endpoints `/health/live` (process) and `/health/ready` (DB connectivity via `AlertDbContext`); migration script regenerated from EF migrations

## Testing

- Main test layers: unit tests for controllers (Moq'd `IAlertService`) and services (Moq'd repository); repository tests against EF Core in-memory/SQLite; health-check tests via `WebApplicationFactory`
- Test frameworks: xUnit 2.9.3, Moq 4.20.72, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory/SQLite, coverlet.collector
- Test naming / placement pattern: one test project per production area (`<Project>.Tests`), mirroring source folders (`Controllers/`, `Services/`, `Repositories/`), files named `<ClassUnderTest>Tests.cs`

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, dependency flow, API routes/query params, response examples, health checks |
| `AlertService.API/AlertService.API.http` | API request samples | Manual endpoint calls |
| `standards/*.md` | Engineering standards | Full standards source (see `standards-summary.md`) |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts: `NOT_AVAILABLE` (Swagger generated at runtime in Development)
- Lint / format configuration: `NOT_CONFIGURED`
- Authentication / authorization: `TO_BE_DISCOVERED` (none seen in `Program.cs`)
