# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: Repository inspection (existing code)

## Overview

- Product: AlertService — a small but enterprise-structured microservice for managing alerts (CRUD, filtering/paging, severity summary, health probes).
- Primary stack: C# / .NET 8 (`net8.0`), ASP.NET Core Web API, EF Core 8 + SQL Server, Serilog, Swagger; tested with xUnit + Moq (+ EF Core InMemory).
- Repository shape: multi-project .NET solution (`AlertService.sln`) with layered projects (API, DTO, Models, Common, Data abstractions, Data.SQL implementation) plus two test projects.
- Shared build/runtime settings: `Directory.Build.props` applies `net8.0`, `ImplicitUsings=enable`, `Nullable=enable`, `TreatWarningsAsErrors=false` to every project.

## Requirements Summary

Not applicable (EXISTING_PROJECT).

## Architecture

- Layering: HTTP/API → business service → repository abstraction → SQL implementation → domain models/common. Dependency flow: `API → DTO → Common`; `API → Data (interfaces) → Models → Common`; `API → Data.SQL → Data`.
- Entry points / composition roots: `AlertService.API/Program.cs` (DI registration, Serilog, middleware, migrations-on-startup in Development, health endpoints, controllers).
- User-facing or external interfaces: REST API under `/api/alerts` (POST/GET/GET summary/GET by id/PUT/PATCH deactivate/DELETE); health probes `/health/live` and `/health/ready`; Swagger UI in Development.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` (behind `IAlertService`); entity↔DTO mapping in `AlertService.API/Mappings/AlertMappingExtensions.cs`.
- Data / integration boundaries: `AlertService.Data` holds `IAlertRepository`; `AlertService.Data.SQL` implements it with `AlertDbContext`, EF configurations, and migrations. Service layer depends only on `IAlertRepository`.
- Notable dependency flow: the API project references `Data.SQL` solely to register it in DI via `services.AddSqlDataAccess(configuration)`.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services (composition root) | Program.cs, controllers, services, mappings, middleware, health-check extensions |
| `AlertService.API/Controllers/AlertsController.cs` | REST controller | HTTP concerns only: routing, status codes |
| `AlertService.API/Services/` | Business logic | `IAlertService` + `AlertManagementService` |
| `AlertService.API/Middleware/ExceptionHandlingMiddleware.cs` | Cross-cutting | Unhandled errors → 500 ProblemDetails |
| `AlertService.DTO/` | Request/response contracts | `Requests/`, `Responses/` |
| `AlertService.Models/` | Domain entities | `Alert` |
| `AlertService.Common/` | Shared enums/constants | `Severity`, field-length constants |
| `AlertService.Data/` | Data-access abstractions | `IAlertRepository` interface |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext`, `Configurations/`, `Repositories/`, `Migrations/`, DI extensions |
| `database/` | SQL assets | `01_CreateDatabase.sql`, idempotent `02_AlertServiceDb_Migrations.sql` |
| `AlertService.API.Tests/` | Controller + service unit tests | xUnit, Moq; also health-check tests |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit, EF Core InMemory |
| `standards/` | Engineering standards | Source for the SDLC standards catalog |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`.
- Build / compile: `dotnet build AlertService.sln`.
- Test: `dotnet test`.
- Coverage: `NOT_CONFIGURED` (no explicit coverage tooling detected).
- Lint / format: `NOT_CONFIGURED` (no analyzer/format config beyond compiler defaults; `TreatWarningsAsErrors=false`).
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https` (Swagger at `https://localhost:7080/swagger`).

## Data And Operations

- Primary data store: SQL Server (LocalDB by default); connection string `ConnectionStrings:AlertDb` in `AlertService.API/appsettings.json`.
- Environment / config notes: `Database:ApplyMigrationsOnStartup` (true in `appsettings.Development.json`) applies EF migrations at startup; EF tool pinned via `dotnet-tools.json` (dotnet-ef 8.0.31). Target `net8.0`; set `DOTNET_ROLL_FORWARD=Major` to run on a newer runtime.
- Operational assets: Serilog to console and `AlertService.API/Logs/alertservice-<date>.log`; health probes `/health/live` (liveness) and `/health/ready` (DB connectivity); `database/` SQL scripts for DBA/CI provisioning.

## Testing

- Main test layers: controller + service unit tests (`AlertService.API.Tests/`); repository tests (`AlertService.Data.SQL.Tests/`); health-check tests via `WebApplicationFactory<Program>`.
- Test frameworks: xUnit, Moq, EF Core InMemory.
- Test naming / placement pattern: one test project per layer, mirroring source folders (`Controllers/`, `Services/`, `Repositories/`, `TestInfrastructure/`).

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Architecture overview, API/health reference, run/test and EF migration commands |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts (OpenAPI file): `NOT_AVAILABLE` (Swagger generated at runtime only)
- Coverage / lint tooling: `NOT_CONFIGURED`

