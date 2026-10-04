# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this file; `/refresh-repo-context` patches stale sections.
> Stages read this file only for missing facts recorded in `work.json`.

- Source: `repository inspection (EXISTING_PROJECT)`

## Overview

- Product: Alert management microservice providing CRUD, filtering, paging, summary, and health endpoints.
- Primary stack: .NET 8, ASP.NET Core Web API, EF Core (SQL Server), Serilog.
- Repository shape: multi-project solution with layered separation (`API`, `Services`, `Data`, `Data.SQL`, `DTO`, `Models`, `Common`, tests).
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, implicit usings enabled.

## Requirements Summary

Only for `NEW_PROJECT`: compact summary of the requirements document.

- Not applicable (`EXISTING_PROJECT`).

## Architecture

- Layering: HTTP controllers -> service layer (`IAlertService` / `AlertManagementService`) -> repository abstraction (`AlertService.Data`) -> EF Core SQL implementation (`AlertService.Data.SQL`).
- Entry points / composition roots: `AlertService.API/Program.cs`.
- User-facing interfaces: REST API endpoints in `AlertService.API/Controllers/AlertsController.cs`; health endpoints under `/health/live` and `/health/ready`.
- Business logic / orchestration: `AlertService.API/Services/`.
- Data / integration boundaries: SQL Server via `AlertService.Data.SQL/AlertDbContext.cs` and repositories; migrations in `AlertService.Data.SQL/Migrations/` and SQL scripts in `database/`.
- Notable dependency flow: API references DTO, Models, Data abstractions, and Data.SQL only for DI wiring; service depends on repository interfaces.

## Repository Map

Important areas and control points only.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | Web API host and composition root | Controllers, middleware, DI registration, health endpoint mapping, Swagger setup |
| `AlertService.API/Services/` | Business/service layer | Core alert behavior and orchestration boundaries |
| `AlertService.Data/` | Persistence abstractions | Repository interfaces used by service layer |
| `AlertService.Data.SQL/` | SQL Server persistence implementation | DbContext, EF configuration, repositories, migrations |
| `AlertService.DTO/` | API request/response contracts | External contract DTOs |
| `AlertService.Models/` | Domain entities | Entity model shared by service and data layers |
| `AlertService.Common/` | Cross-cutting constants/enums | Shared types such as severity enum/constants |
| `AlertService.API.Tests/` | API/service test suite | xUnit + Moq tests, plus integration-style health checks |
| `AlertService.Data.SQL.Tests/` | Persistence test suite | Repository behavior tests with EF in-memory/sqlite packages |
| `database/` | SQL scripts and migration artifacts | Database creation and idempotent migration script |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build` (or `dotnet build AlertService.sln`)
- Test: `dotnet test`
- Coverage: `TO_BE_DISCOVERED` (collector package exists; command/threshold policy not explicitly documented)
- Lint / format: `NOT_CONFIGURED` (no dedicated lint/format command discovered)
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server (`ConnectionStrings:AlertDb` in `AlertService.API/appsettings.json`).
- Environment / config notes: optional startup migration toggle via `Database:ApplyMigrationsOnStartup`; Serilog console + rolling file logging.
- Operational assets: health endpoints (`/health/live`, `/health/ready`), Swagger in development, EF Core migration scripts in `database/`.

## Testing

- Main test layers: API/controller/service tests and data/repository tests.
- Test frameworks: xUnit, Moq, `Microsoft.NET.Test.Sdk`; additional packages include `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory/Sqlite.
- Test naming / placement pattern: test projects mirror source areas (`Controllers/`, `Services/`, `Repositories/` and infrastructure helpers).

## Project Docs

Index only — never copy document bodies.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Architecture overview, API behavior, local run/test steps, migration workflow |
| `.github/copilot-instructions.md` | SDLC process doc | Manual skill-stage pipeline and artifact ownership rules |
| `standards/*.md` | Engineering standards docs | Rules for coding, backend, API, database, service architecture, and UI/front-end |

## Known Gaps

- BRDs / formal requirements doc in repository: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition in repository docs: `TO_BE_DISCOVERED`
- Deployment runbook: `NOT_AVAILABLE`
- Separate OpenAPI contract file (`openapi*.yaml`/`swagger*.yaml`): `NOT_AVAILABLE`
