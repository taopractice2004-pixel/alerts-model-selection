# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`README.md`, `*.csproj`, `Directory.Build.props`, `Program.cs`, `.gitignore`)

## Overview

- Product: AlertService — alert management microservice (CRUD, paged/filtered listing, summary counts, deactivate, health probes)
- Primary stack: C# / ASP.NET Core Web API (.NET 8), EF Core 8 + SQL Server, Serilog, Swashbuckle
- Repository shape: single .NET solution (`AlertService.sln`), 7 source projects + 2 test projects, plus `database/` SQL scripts
- Shared build/runtime settings: `Directory.Build.props` — `net8.0`, `ImplicitUsings` enabled, `Nullable` enabled, `TreatWarningsAsErrors` false

## Requirements Summary

Not applicable

## Architecture

- Layering: Controller (HTTP only) → Service (`IAlertService`) → Repository (`IAlertRepository`) → EF Core `AlertDbContext`
- Entry points / composition roots: `AlertService.API/Program.cs` (DI, Serilog, Swagger in Development, middleware, health endpoints, optional migrations on startup via `Database:ApplyMigrationsOnStartup`)
- User-facing or external interfaces: REST `/api/alerts` (POST, GET paged, GET `/summary`, GET/PUT/DELETE `/{id}`, PATCH `/{id}/deactivate`); health `/health/live`, `/health/ready`
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs`
- Data / integration boundaries: `AlertService.Data` (interfaces) and `AlertService.Data.SQL` (implementation, registered via `AddSqlDataAccess`)
- Notable dependency flow: API → DTO → Common; API → Data (interfaces) → Models → Common; API → Data.SQL → Data. Mapping in `Mappings/AlertMappingExtensions.cs`; unhandled errors → 500 ProblemDetails via `ExceptionHandlingMiddleware`

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer, services, composition root | `Controllers/`, `Services/`, `Mappings/`, `Middleware/`, `Extensions/` (health checks) |
| `AlertService.DTO/` | Request/response contracts | `Requests/`, `Responses/` (incl. `PagedResponse`) — API contract changes land here |
| `AlertService.Models/` | Domain entity `Alert` | |
| `AlertService.Common/` | Shared enums/constants | `Severity`, `AlertConstants` (field lengths) |
| `AlertService.Data/` | Repository abstractions | `Interfaces/IAlertRepository.cs` |
| `AlertService.Data.SQL/` | EF Core SQL Server implementation | `AlertDbContext`, `Configurations/`, `Repositories/`, `Migrations/`, `Extensions/ServiceCollectionExtensions.cs` |
| `AlertService.API.Tests/` | Controller, service and health-check tests | xUnit + Moq; `TestInfrastructure/` web app factory |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit, EF Core InMemory/SQLite |
| `database/` | SQL scripts | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` (generated from EF migrations) |
| `standards/` | Full coding standards | Routed via `manifest.json` → `standards.items` |

## Build And Run

- Install / restore dependencies: `dotnet restore AlertService.sln`; `dotnet tool restore` (dotnet-ef 8.0.31)
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test AlertService.sln`
- Coverage: `dotnet test AlertService.sln --collect:"XPlat Code Coverage"` (`coverlet.collector` in API.Tests only)
- Lint / format: `NOT_CONFIGURED` (no `.editorconfig` in the repository)
- Static analysis / code metrics: `NOT_CONFIGURED` — no analyzer packages or `.editorconfig` rules; only compiler warnings and nullable checks from `Directory.Build.props`
- Run main app locally: `dotnet run --project AlertService.API` (sample requests in `AlertService.API.http`; Swagger UI in Development)

## Data And Operations

- Primary data store: SQL Server (`AlertServiceDb`), EF Core migrations in `AlertService.Data.SQL/Migrations`
- Environment / config notes: `ConnectionStrings:AlertDb` (LocalDB default in `appsettings.json`); `Database:ApplyMigrationsOnStartup` (default false); Serilog console + rolling file `Logs/alertservice-.log`
- Operational assets: health probes `/health/live` and `/health/ready`; migration SQL script in `database/`

## Testing

- Main test layers: controller tests, service tests (mocked repository), repository tests (InMemory/SQLite), health check integration tests (`WebApplicationFactory`)
- Test frameworks: xUnit 2.9.3, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory/Sqlite
- Test naming / placement pattern: mirrored folder per layer in `<Project>.Tests/` (`Controllers/`, `Services/`, `Repositories/`), file `<ClassUnderTest>Tests.cs`

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, dependency flow, API routes/examples, health checks |
| `standards/*.md` | Standards | Coding, backend .NET, API REST, database, service architecture, code review (frontend-react and ui present but not applicable) |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts (OpenAPI files): `NOT_AVAILABLE` (Swagger generated at runtime)
- Lint / static analysis config: `NOT_CONFIGURED`
