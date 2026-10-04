# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (EXISTING_PROJECT)

## Overview

- Product: AlertService — an alert management microservice (create, query, update, deactivate, delete alerts) exposed as a REST API.
- Primary stack: .NET 8 / C#, ASP.NET Core Web API, EF Core 8 on SQL Server, Serilog, Swagger, xUnit + Moq.
- Repository shape: single .NET solution (`AlertService.sln`) with layered projects (API, Services, DTO, Models, Common, Data abstractions, Data.SQL) plus two test projects.
- Shared build/runtime settings: `Directory.Build.props` — `net8.0`, `ImplicitUsings=enable`, `Nullable=enable`, `TreatWarningsAsErrors=false`.

## Requirements Summary

Not applicable (EXISTING_PROJECT).

## Architecture

- Layering: Delivery (API controllers + middleware) → business (service layer) → persistence (repository abstraction in `AlertService.Data`, EF Core implementation in `AlertService.Data.SQL`). DTO/Models/Common are shared contracts and types.
- Entry points / composition roots: `AlertService.API/Program.cs` (DI registration, Serilog, Swagger, health checks, migration-on-startup, HTTP pipeline).
- User-facing or external interfaces: REST API under `/api/alerts` (CRUD + `summary` + `deactivate`); health probes `/health/live` and `/health/ready`; Swagger UI in Development.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` (implements `IAlertService`), depends only on `IAlertRepository`.
- Data / integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs` (contract) implemented by `AlertService.Data.SQL/Repositories/AlertRepository.cs` over `AlertDbContext`.
- Notable dependency flow: `API → DTO → Common`; `API → Data (interfaces) → Models → Common`; `API → Data.SQL → Data`. The API references `Data.SQL` only to register it in DI (`AddSqlDataAccess`).

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services (composition root) | Controllers, service layer, mappings, middleware, `Program.cs` |
| `AlertService.API/Controllers/AlertsController.cs` | REST endpoints | HTTP only: routing, status codes |
| `AlertService.API/Services/` | Business logic | `IAlertService` + `AlertManagementService` |
| `AlertService.Data/` | Persistence abstraction | `IAlertRepository` interface |
| `AlertService.Data.SQL/` | EF Core / SQL Server implementation | `AlertDbContext`, `Configurations/`, `Repositories/`, `Migrations/`, `Extensions/` |
| `AlertService.DTO/` | Request/response contracts | `Requests/`, `Responses/` |
| `AlertService.Models/` | Domain entities | `Alert` |
| `AlertService.Common/` | Shared enums/constants | `Severity`, field-length constants |
| `database/` | SQL scripts | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` |
| `AlertService.API.Tests/` | Controller + service unit tests | xUnit, Moq, `WebApplicationFactory` |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit, EF Core (Sqlite/InMemory) |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `coverlet.collector` is referenced; `dotnet test --collect:"XPlat Code Coverage"`
- Lint / format: `NOT_CONFIGURED` (no analyzer/format config found; `TreatWarningsAsErrors=false`)
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https` (Swagger: `https://localhost:7080/swagger`)

## Data And Operations

- Primary data store: SQL Server, accessed via EF Core 8 (`AlertDbContext`). Default connection targets LocalDB (`ConnectionStrings:AlertDb`).
- Environment / config notes: `appsettings.json` / `appsettings.Development.json`; `Database:ApplyMigrationsOnStartup` applies migrations on startup in Development. EF tooling pinned via `dotnet-tools.json` (dotnet-ef 8.0.31).
- Operational assets: Serilog logs to console and `AlertService.API/Logs/alertservice-<date>.log`; health probes for orchestrators; idempotent migration script in `database/`.

## Testing

- Main test layers: controller + service unit tests (`AlertService.API.Tests`), repository tests (`AlertService.Data.SQL.Tests`), health-check tests.
- Test frameworks: xUnit, Moq; `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`); `Microsoft.EntityFrameworkCore.Sqlite` for repository tests.
- Test naming / placement pattern: one test project per production area, mirroring folders (e.g. `Controllers/AlertsControllerTests.cs`, `Services/AlertManagementServiceTests.cs`).

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Overview, folder structure, API/health-check reference, run and EF migration commands |
| `standards/` | Engineering standards | Coding, backend .NET, API REST, service architecture, database (+ frontend/UI, not applicable here) |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts (OpenAPI file): `NOT_AVAILABLE` (Swagger generated at runtime via Swashbuckle)
- Lint/format configuration: `NOT_CONFIGURED`
