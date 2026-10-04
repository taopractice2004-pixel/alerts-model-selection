# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection

## Overview

- Product: AlertService — alert management microservice (create, list/filter/page/sort, summary, update, deactivate, delete)
- Primary stack: ASP.NET Core Web API (.NET 8), EF Core 8 + SQL Server, Serilog, Swagger, xUnit + Moq
- Repository shape: single solution (`AlertService.sln`), 8 projects (5 libraries/API, 2 test projects, 1 data-abstraction project)
- Shared build/runtime settings: `Directory.Build.props` — `net8.0`, implicit usings, nullable enabled, `TreatWarningsAsErrors=false`; `DOTNET_ROLL_FORWARD=Major` needed if only a newer runtime is installed

## Requirements Summary

Not applicable

## Architecture

- Layering: API (controllers + business services) → DTO/Common; API → Data (interfaces) → Models; API → Data.SQL (EF Core implementation) → Data
- Entry points / composition roots: `AlertService.API/Program.cs` (DI, Serilog, Swagger, health checks, middleware, optional migrate-on-startup)
- User-facing or external interfaces: REST `/api/alerts` (POST, GET paged, GET `/summary`, GET/PUT/DELETE `/{id}`, PATCH `/{id}/deactivate`); health probes `/health/live`, `/health/ready`
- Business logic / orchestration locations: `AlertService.API/Services/` (`IAlertService`, `AlertManagementService`)
- Data / integration boundaries: services depend only on `IAlertRepository` (`AlertService.Data/Interfaces`); `AlertService.Data.SQL` registers via `services.AddSqlDataAccess(configuration)`
- Notable dependency flow: entity <-> DTO mapping in `AlertService.API/Mappings/AlertMappingExtensions.cs`; unhandled errors -> 500 ProblemDetails via `Middleware/ExceptionHandlingMiddleware.cs`

## Repository Map

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + services, composition root | Controllers, Services, Mappings, Middleware, Extensions (health checks), appsettings*.json |
| `AlertService.Common/` | Shared enums/constants | `Severity`, `AlertConstants` (field lengths) |
| `AlertService.DTO/` | Request/response contracts | `Requests/` (Create/Update/AlertQuery), `Responses/` (Alert, PagedResponse, summary) |
| `AlertService.Models/` | Domain entities | `Alert` |
| `AlertService.Data/` | Data-access abstractions | `Interfaces/IAlertRepository` |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext`, `Configurations/`, `Repositories/AlertRepository`, `Extensions/ServiceCollectionExtensions`, `Migrations/` |
| `AlertService.API.Tests/` | Controller, service, health-check tests | `Controllers/`, `Services/`, `TestInfrastructure/` (WebApplicationFactory) |
| `AlertService.Data.SQL.Tests/` | Repository tests | `Repositories/AlertRepositoryTests.cs` |
| `database/` | SQL scripts | `01_CreateDatabase.sql`, idempotent `02_AlertServiceDb_Migrations.sql` generated from EF migrations |
| `standards/` | Engineering standards | See `standards-summary.md` |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test AlertService.sln` (single project: `dotnet test AlertService.API.Tests` or `AlertService.Data.SQL.Tests`)
- Coverage: `dotnet test AlertService.sln --collect:"XPlat Code Coverage"` (`coverlet.collector` in `AlertService.API.Tests`)
- Lint / format: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https` (Swagger at `https://localhost:7080/swagger`)

## Data And Operations

- Primary data store: SQL Server via EF Core (`ConnectionStrings:AlertDb`, default LocalDB `AlertServiceDb`)
- Environment / config notes: `appsettings.json`, `appsettings.Development.json` (`Database:ApplyMigrationsOnStartup=true`); logs to console and `AlertService.API/Logs/alertservice-<date>.log`
- Operational assets: EF migrations (`dotnet ef ... --project AlertService.Data.SQL --startup-project AlertService.API --output-dir Migrations`), `database/*.sql`, health probes for Kubernetes

## Testing

- Main test layers: unit tests for controllers/services (Moq), repository tests (EF Core InMemory/SQLite), health-check tests via `WebApplicationFactory`
- Test frameworks: xUnit 2.9.3, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory/Sqlite 8.0.31, coverlet.collector
- Test naming / placement pattern: `<Project>.Tests/<Layer folder>/<ClassUnderTest>Tests.cs` mirroring the source folder

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, dependency flow, API contract, health checks, run/migration/test commands |
| `standards/*.md` | Engineering standards | Full source for compact rules in `.github/instructions/standards/` |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts (OpenAPI file): `NOT_AVAILABLE` (Swagger generated at runtime)
- Lint / format tooling: `NOT_CONFIGURED`
