# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`/setup-repo-context 1`)

## Overview

- Product: AlertService — alert management microservice (create/query/update/deactivate/delete alerts, summary, health probes)
- Primary stack: C# / .NET 8, ASP.NET Core Web API, EF Core 8.0.31 (SQL Server), Serilog, Swashbuckle
- Repository shape: single .NET solution (`AlertService.sln`) with 6 source projects + 2 test projects
- Shared build/runtime settings: `Directory.Build.props` — `net8.0`, `ImplicitUsings` and `Nullable` enabled, `TreatWarningsAsErrors=false`

## Requirements Summary

Not applicable (`EXISTING_PROJECT`).

## Architecture

- Layering: Controller (HTTP only) → Service (`IAlertService`) → Repository (`IAlertRepository`) → EF Core `AlertDbContext`; entities ↔ DTOs mapped in `Mappings/`
- Entry points / composition roots: `AlertService.API/Program.cs` (DI, Serilog, Swagger in Development, optional migrate-on-startup via `Database:ApplyMigrationsOnStartup`)
- User-facing or external interfaces: REST `/api/alerts` (POST, GET paged, GET `/summary`, GET/PUT/DELETE `/{id}`, PATCH `/{id}/deactivate`); health `/health/live`, `/health/ready`
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs`
- Data / integration boundaries: `AlertService.Data` (interfaces) and `AlertService.Data.SQL` (SQL Server impl, DI via `AddSqlDataAccess`)
- Notable dependency flow: API → DTO → Common; API → Data → Models → Common; API → Data.SQL → Data. `ExceptionHandlingMiddleware` maps unhandled errors to 500 ProblemDetails; enums serialized as strings

## Repository Map

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services, composition root | `Controllers/`, `Services/`, `Mappings/`, `Middleware/`, `Extensions/` (health checks), `Program.cs`, `appsettings*.json` |
| `AlertService.Common/` | Shared enums/constants | `Enums/Severity.cs`, `Constants/AlertConstants.cs` (field lengths) |
| `AlertService.DTO/` | Request/response contracts | `Requests/` (Create/Update/AlertQuery), `Responses/` (Alert, Paged, Summary, SeverityCounts) |
| `AlertService.Models/` | Domain entities | `Alert.cs` |
| `AlertService.Data/` | Data-access abstractions | `Interfaces/IAlertRepository.cs` |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext.cs`, `Configurations/`, `Repositories/AlertRepository.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Migrations/` |
| `AlertService.API.Tests/` | Controller, service and health-check tests | `Controllers/`, `Services/`, `TestInfrastructure/`, `HealthChecksTests.cs` |
| `AlertService.Data.SQL.Tests/` | Repository tests | `Repositories/AlertRepositoryTests.cs` |
| `database/` | SQL deliverables | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` (idempotent, generated from EF migrations) |
| `standards/` | Engineering standards (source of truth) | Compact rules in `.github/instructions/standards/` |

## Build And Run

- Install / restore dependencies: `dotnet restore AlertService.sln`; `dotnet tool restore` (dotnet-ef 8.0.31)
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test AlertService.sln`
- Coverage: `dotnet test AlertService.sln --collect:"XPlat Code Coverage"` (coverlet.collector present in `AlertService.API.Tests` only)
- Lint / format: `NOT_CONFIGURED` (no analyzers/`.editorconfig` config discovered; `dotnet format` not wired)
- Run main app locally: `dotnet run --project AlertService.API` (Swagger in Development; `AlertService.API.http` for sample requests)

## Data And Operations

- Primary data store: SQL Server via EF Core; connection string `ConnectionStrings:AlertDb` (LocalDB `AlertServiceDb` in `appsettings.json`); migrations in `AlertService.Data.SQL/Migrations/` (`InitialCreate`)
- Environment / config notes: `appsettings.json` / `appsettings.Development.json`; Serilog console + rolling file `Logs/alertservice-.log`; `Database:ApplyMigrationsOnStartup` defaults to `false`
- Operational assets: health endpoints `/health/live` (process) and `/health/ready` (DB connectivity); no Dockerfile or CI definition discovered

## Testing

- Main test layers: unit tests for controllers and services (Moq); repository tests (EF Core InMemory/Sqlite); health-check integration-style tests via `WebApplicationFactory`
- Test frameworks: xUnit 2.9.3, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory/Sqlite 8.0.31, coverlet.collector
- Test naming / placement pattern: `<ProjectUnderTest>.Tests/<Layer>/<ClassName>Tests.cs`; infrastructure helpers in `TestInfrastructure/`

## Project Docs

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, dependency flow, API routes/examples, health checks |
| `standards/*.md` | Engineering standards | Coding, backend .NET, REST API, database, service architecture (frontend/UI present but not applicable) |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts (OpenAPI YAML in source control): `NOT_AVAILABLE` (Swagger generated at runtime only; API standard prefers contract-first OpenAPI and URL versioning, current routes are unversioned `/api/alerts`)
- Coverage tooling in `AlertService.Data.SQL.Tests`: `TO_BE_DISCOVERED` (no coverlet reference)
- Test project `AlertService.Data.SQL.Tests` has no Moq reference
