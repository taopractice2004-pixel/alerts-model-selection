# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`EXISTING_PROJECT`)

## Overview

- Product: AlertService alert management microservice
- Primary stack: ASP.NET Core Web API (.NET 8), EF Core 8, SQL Server, Serilog, Swagger
- Repository shape: multi-project .NET solution with layered API, service, data abstractions, SQL implementation, shared contracts/models, and dedicated test projects
- Shared build/runtime settings: `Directory.Build.props` enforces `net8.0`, nullable and implicit usings

## Requirements Summary

Only for `NEW_PROJECT`: compact summary of the requirements document — key features, external
interfaces, data, and non-functional constraints. Write `Not applicable` for `EXISTING_PROJECT`.

- Not applicable (`EXISTING_PROJECT`)

## Architecture

For `NEW_PROJECT`, this and the Repository Map describe the **planned** structure from the
requirements document until code exists.

- Layering: controller (HTTP) -> service (business logic) -> repository abstraction -> EF Core SQL implementation
- Entry points / composition roots: `AlertService.API/Program.cs`
- User-facing or external interfaces (UI/API/CLI/background): REST API in `AlertService.API/Controllers/AlertsController.cs`, health probes at `/health/live` and `/health/ready`
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs`
- Data / integration boundaries: repository contracts in `AlertService.Data`, SQL EF implementation in `AlertService.Data.SQL`, SQL scripts in `database/`
- Notable dependency flow: API depends on DTO/Common/Models/Data interfaces and registers Data.SQL via DI; tests target API and Data.SQL separately

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.sln` | Solution root | Primary build and test entry point |
| `Directory.Build.props` | Shared build settings | Sets target framework and compiler defaults |
| `AlertService.API/` | HTTP entrypoint and composition root | Controllers, middleware, health endpoints, DI setup |
| `AlertService.API/Services/` | Business logic layer | Alert management orchestration and validation |
| `AlertService.Data/` | Persistence contracts | Repository interfaces and boundary abstractions |
| `AlertService.Data.SQL/` | SQL persistence implementation | DbContext, repository, EF migrations, SQL Server provider |
| `AlertService.DTO/` | API request/response contracts | Payload contracts and paging/summary responses |
| `AlertService.Models/` | Domain entities | Shared domain model definitions |
| `AlertService.Common/` | Shared constants/enums | Severity enum and constants reused across layers |
| `AlertService.API.Tests/` | API and service tests | xUnit/Moq controller and service behavior tests |
| `AlertService.Data.SQL.Tests/` | Persistence tests | xUnit repository tests with EF test providers |
| `database/` | Database operations artifacts | Create DB and idempotent migration SQL scripts |
| `standards/` | Full standards sources | Inputs for standards routing and stage selection |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` (`coverlet.collector` installed)
- Lint / format: `NOT_CONFIGURED` (no dedicated lint/format command discovered)
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core (`Microsoft.EntityFrameworkCore.SqlServer`)
- Environment / config notes: connection string in `AlertService.API/appsettings*.json`; optional startup migrations in Development
- Operational assets: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`, structured logs under `AlertService.API/Logs/`

## Testing

- Main test layers: API/controller and service unit tests, repository data-access tests
- Test frameworks: xUnit, Moq, ASP.NET Core test host, EF Core InMemory/SQLite providers
- Test naming / placement pattern: tests grouped by layer under `AlertService.API.Tests/` and `AlertService.Data.SQL.Tests/`

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Local setup, API behavior, run/build/test/migration commands |
| `standards/` | Engineering standards catalog | Source standards referenced by `.sdlc/context/standards-summary.md` and routing in `manifest.json` |
| `database/` | Operations scripts | Database creation and migration script artifacts |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE` (only implementation-focused repository docs discovered)
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `TO_BE_DISCOVERED` (pipeline file not verified in this run)
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts: `NOT_AVAILABLE` (no standalone OpenAPI file discovered; Swagger generated from code)
