# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`README.md`, solution and project files, sample source)

## Overview

- Product: Alert management microservice exposing a REST API for CRUD, summary, filtering, paging, and health checks.
- Primary stack: .NET 8 ASP.NET Core Web API, EF Core 8, SQL Server, Serilog, Swagger, xUnit, Moq.
- Repository shape: multi-project solution with API, shared contracts/models, data abstractions, SQL persistence implementation, tests, database scripts, and standards.
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, implicit usings enabled, warnings not treated as errors.

## Requirements Summary

Not applicable for `EXISTING_PROJECT`.

## Architecture

- Layering: controller layer in `AlertService.API`, service layer in `AlertService.API/Services`, repository abstractions in `AlertService.Data`, EF Core implementation in `AlertService.Data.SQL`, shared enums/constants in `AlertService.Common`, DTOs in `AlertService.DTO`, and domain entities in `AlertService.Models`.
- Entry points / composition roots: `AlertService.API/Program.cs` configures logging, DI, middleware, health checks, migrations, Swagger, and controller endpoints.
- User-facing or external interfaces: HTTP REST API, Swagger UI in development, readiness/liveness endpoints, and SQL Server connectivity.
- Business logic / orchestration locations: `AlertManagementService` owns alert rules such as trimming, UTC timestamps, update semantics, and logging.
- Data / integration boundaries: `IAlertRepository` abstractions isolate persistence; `AlertDbContext` and SQL repositories encapsulate EF Core and database access.
- Notable dependency flow: API -> DTO/Common plus Data abstractions; API composition root references `AlertService.Data.SQL` only for registration and migrations.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | Web API host and composition root | Contains controllers, services, middleware, mappings, health checks, config, and app startup. |
| `AlertService.Data/` | Persistence abstractions | Keeps repository contracts separate from EF Core implementation. |
| `AlertService.Data.SQL/` | SQL Server persistence | Holds `AlertDbContext`, EF Core configuration, repositories, migrations, and DI extensions. |
| `AlertService.Common/` | Shared primitives | Central place for enums and constants used across layers. |
| `AlertService.DTO/` | Request and response contracts | External API payload contracts. |
| `AlertService.Models/` | Domain entities | Core alert model used by service and persistence layers. |
| `AlertService.API.Tests/` | API/service tests | xUnit, Moq, integration-style host tests, and health check coverage. |
| `AlertService.Data.SQL.Tests/` | Persistence tests | Repository-focused tests using EF Core test providers. |
| `database/` | SQL operational artifacts | Database creation and idempotent migration scripts. |
| `standards/` | Full standards source | Human-readable engineering standards summarized in `.sdlc/context/standards-summary.md`. |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `NOT_CONFIGURED` as a repository-level command; `coverlet.collector` is referenced in `AlertService.API.Tests`
- Lint / format: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core SQL Server provider.
- Environment / config notes: connection string lives under `ConnectionStrings:AlertDb`; development can apply migrations on startup via `Database:ApplyMigrationsOnStartup`.
- Operational assets: `database/*.sql`, `AlertService.API/AlertService.API.http`, Swagger in development, and `/health/live` plus `/health/ready` endpoints.

## Testing

- Main test layers: controller and service tests in `AlertService.API.Tests`; repository and DbContext-backed tests in `AlertService.Data.SQL.Tests`.
- Test frameworks: xUnit, Moq, ASP.NET Core MVC testing utilities, EF Core SQLite/InMemory test providers, coverlet collector.
- Test naming / placement pattern: per-project `*.Tests` companions with feature-area subfolders matching production structure.

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Repository overview, architecture summary, API surface, local run steps, and EF migration commands. |
| `standards/api-rest-standards.md` | Standards doc | REST API design and contract guidance for controller and contract changes. |
| `standards/backend-dotnet-standards.md` | Standards doc | Backend C# coding and layering expectations. |
| `standards/database-standards.md` | Standards doc | Persistence, schema, and query review guidance. |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts: `NOT_AVAILABLE` as checked-in OpenAPI files; runtime Swagger generation exists
- Repository-wide lint or formatting command: `NOT_CONFIGURED`
