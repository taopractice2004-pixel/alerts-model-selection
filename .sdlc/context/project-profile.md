# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`EXISTING_PROJECT`)

## Overview

- Product: AlertService — an alert management microservice (HTTP API for creating, querying,
  updating, deactivating and deleting alerts, plus a summary aggregate).
- Primary stack: C# / .NET 8 (`net8.0`), ASP.NET Core Web API, EF Core 8 on SQL Server.
- Repository shape: single solution (`AlertService.sln`) of layered class-library projects plus
  one API host and two test projects.
- Shared build/runtime settings: `Directory.Build.props` applies to all projects —
  `TargetFramework=net8.0`, `ImplicitUsings=enable`, `Nullable=enable`,
  `TreatWarningsAsErrors=false`.

## Requirements Summary

Not applicable (`EXISTING_PROJECT`).

## Architecture

- Layering: clean layered design — API (HTTP + business services) → DTO → Common; API → Data
  (interfaces) → Models → Common; Data.SQL implements Data. Service layer depends only on
  `IAlertRepository`.
- Entry points / composition roots: `AlertService.API/Program.cs` (minimal hosting, DI, Serilog,
  Swagger, middleware, health endpoints, migrations-on-startup in Development).
- User-facing or external interfaces: REST API under `/api/alerts` plus `/health/live` and
  `/health/ready` probe endpoints; Swagger UI in Development.
- Business logic / orchestration locations:
  `AlertService.API/Services/AlertManagementService.cs` (implements `IAlertService`);
  entity↔DTO mapping in `AlertService.API/Mappings/AlertMappingExtensions.cs`.
- Data / integration boundaries: `IAlertRepository` (in `AlertService.Data`) implemented by
  `AlertService.Data.SQL/Repositories/AlertRepository.cs` over `AlertDbContext` (SQL Server).
- Notable dependency flow: API registers SQL data access via `services.AddSqlDataAccess(configuration)`;
  API knows `Data.SQL` only for DI registration.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services + composition root | Controllers, Services, Mappings, Middleware, `Program.cs`, appsettings |
| `AlertService.API/Controllers/AlertsController.cs` | REST controller | Routing and status codes only |
| `AlertService.API/Services/` | Business logic | `IAlertService` + `AlertManagementService` |
| `AlertService.DTO/` | Request/response contracts | `Requests/`, `Responses/` (e.g. `PagedResponse`) |
| `AlertService.Models/` | Domain entities | `Alert` |
| `AlertService.Common/` | Shared enums/constants | `Severity` enum, `AlertConstants` |
| `AlertService.Data/` | Data-access abstractions | `IAlertRepository` interface |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext`, configuration, repository, EF migrations |
| `AlertService.API.Tests/` | API + service unit tests | xUnit, Moq, `WebApplicationFactory` |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit, EF Core SQLite/InMemory |
| `database/` | SQL scripts | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`.
- Build / compile: `dotnet build AlertService.sln`.
- Test: `dotnet test`.
- Coverage: `coverlet.collector` (collect via `dotnet test --collect:"XPlat Code Coverage"`).
- Lint / format: no `.editorconfig` or custom analyzer packages found; `dotnet format` available
  via the SDK. `TreatWarningsAsErrors=false`.
- Static analysis / code metrics (repository-defined only; used by `/code-review`): built-in
  .NET SDK analyzers (Roslyn) via the compiler; no additional analyzer packages or
  `.editorconfig` ruleset configured. `NOT_CONFIGURED` beyond SDK defaults.
- Run main app locally:
  `dotnet run --project AlertService.API --launch-profile https` (Swagger at
  `https://localhost:7080/swagger`).

## Data And Operations

- Primary data store: SQL Server via EF Core 8; `AlertDbContext` in `AlertService.Data.SQL`.
- Environment / config notes: connection string `ConnectionStrings:AlertDb` in
  `AlertService.API/appsettings.json`; `Database:ApplyMigrationsOnStartup=true` in Development
  applies migrations at startup. Projects target `net8.0`; set `DOTNET_ROLL_FORWARD=Major` to
  run on a newer-only runtime.
- Operational assets: Serilog logs to console and `AlertService.API/Logs/alertservice-<date>.log`;
  `database/` SQL scripts; `dotnet-ef` local tool (`dotnet-tools.json`, 8.0.31) for migrations;
  health probe endpoints for orchestrators.

## Testing

- Main test layers: API/controller + service unit tests (`AlertService.API.Tests`), repository
  tests (`AlertService.Data.SQL.Tests`), health-check integration tests.
- Test frameworks: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`),
  EF Core SQLite/InMemory, coverlet.
- Test naming / placement pattern: one test project per source project (`<Project>.Tests`),
  mirrored folder layout (`Controllers/`, `Services/`, `Repositories/`), `<Class>Tests.cs`.

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, API reference, health checks, run/test and EF migration commands |
| `database/01_CreateDatabase.sql` | SQL script | Creates the AlertServiceDb database |
| `database/02_AlertServiceDb_Migrations.sql` | SQL script | Idempotent schema script generated from EF migrations |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE` until discovered
- ADRs / architecture decision records: `NOT_AVAILABLE` until discovered
- CI/CD pipeline definition: `NOT_AVAILABLE` until discovered
- Deployment runbook: `NOT_AVAILABLE` until discovered
- Separate API contract artifacts: `NOT_AVAILABLE` (OpenAPI generated at runtime via Swashbuckle; no checked-in spec)
- `.editorconfig` / custom analyzer ruleset: `NOT_AVAILABLE` (SDK-default Roslyn analysis only)
