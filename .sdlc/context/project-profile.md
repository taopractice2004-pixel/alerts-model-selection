# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection

## Overview

- Product: Alert management microservice with CRUD, filtering, and summary APIs.
- Primary stack: .NET 8 ASP.NET Core Web API with EF Core and SQL Server.
- Repository shape: multi-project solution with API, domain models, DTOs, data abstraction, SQL data implementation, and tests.
- Shared build/runtime settings: `net8.0`, nullable enabled, implicit usings enabled in `Directory.Build.props`.

## Requirements Summary

Not applicable for `EXISTING_PROJECT`.

## Architecture

- Layering: API controllers -> service layer -> data repository abstraction -> EF Core SQL implementation.
- Entry points / composition roots: `AlertService.API/Program.cs` configures DI, middleware, health checks, Swagger, and optional startup migrations.
- User-facing or external interfaces (UI/API/CLI/background): REST HTTP API under `/api/alerts` plus health probes `/health/live` and `/health/ready`.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` through `IAlertService`.
- Data / integration boundaries: `AlertService.Data/IAlertRepository` as boundary, `AlertService.Data.SQL` holds `AlertDbContext` and repository implementation.
- Notable dependency flow: API references DTO/Common/Models/Data abstractions and registers Data.SQL in DI.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP API and composition root | Main runtime app, DI setup, middleware, controller endpoints. |
| `AlertService.API/Controllers/` | HTTP endpoint handlers | Route and status behavior for alerts API. |
| `AlertService.API/Services/` | Business service layer | Domain orchestration and business rules. |
| `AlertService.Data/` | Data access contracts | Repository interfaces consumed by service layer. |
| `AlertService.Data.SQL/` | EF Core SQL implementation | DbContext, repository implementation, migrations, SQL Server integration. |
| `AlertService.Models/` | Domain entities | Entity definitions used across data and service layers. |
| `AlertService.DTO/` | API contracts | Request/response DTOs and pagination wrappers. |
| `AlertService.Common/` | Shared primitives | Cross-project enums/constants such as severity. |
| `AlertService.API.Tests/` | API/service tests | xUnit tests for controllers, services, and health endpoints. |
| `AlertService.Data.SQL.Tests/` | Data-layer tests | xUnit tests for repository behavior with EF providers. |
| `database/` | SQL operational scripts | Database creation and idempotent migrations SQL script. |
| `standards/` | Full standards source files | Rule text consumed on demand by downstream stages. |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`.
- Build / compile: `dotnet build AlertService.sln`.
- Test: `dotnet test AlertService.sln`.
- Coverage: `coverlet.collector` is configured in `AlertService.API.Tests`; command for report generation is `TO_BE_DISCOVERED`.
- Lint / format: `NOT_CONFIGURED` (no repo-local formatter/linter command discovered).
- Static analysis / code metrics, per stack — only tools defined in the repository (analyzer packages / `.editorconfig` rules, ESLint config + `devDependency`), never global tools or IDE extensions; used by `/code-review`: .NET compiler diagnostics via build/test and package-provided analyzers transitive to referenced packages; no dedicated analyzer package or `.editorconfig` discovered.
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`.

## Data And Operations

- Primary data store: SQL Server via EF Core in `AlertService.Data.SQL`.
- Environment / config notes: connection string in `AlertService.API/appsettings*.json`; optional `Database:ApplyMigrationsOnStartup` in development settings.
- Operational assets: HTTP probe endpoints (`/health/live`, `/health/ready`) and file/console logging via Serilog (`AlertService.API/Logs/`).

## Testing

- Main test layers: API/controller/service unit tests and repository-focused data tests.
- Test frameworks: xUnit, Moq, `Microsoft.NET.Test.Sdk`, EF Core in-memory/SQLite test providers.
- Test naming / placement pattern: test projects mirror runtime project areas (`Controllers`, `Services`, `Repositories`, and infrastructure helpers).

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Architecture summary, endpoints, local run, migrations, and health-check usage. |
| `standards/*.md` | Engineering standards | Source standards for coding, API, DB, architecture, and review stages. |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`.
- ADRs / architecture decision records: `NOT_AVAILABLE`.
- CI/CD pipeline definition: `NOT_AVAILABLE` (no `.github/workflows/*` file discovered).
- Deployment runbook: `NOT_AVAILABLE`.
- Separate API contract artifacts: `NOT_AVAILABLE` (Swagger generated at runtime, no separate checked-in OpenAPI artifact discovered).
