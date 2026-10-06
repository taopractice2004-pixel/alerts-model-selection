# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`/setup-repo-context 1`)

## Overview

- Product: Alert management microservice exposing a REST API for CRUD, filtering, and summary analytics.
- Primary stack: ASP.NET Core Web API on .NET 8, EF Core 8 with SQL Server, Serilog logging.
- Repository shape: multi-project .NET solution with API, domain/model/DTO/common libraries, data abstraction, SQL data provider, and separate test projects.
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, implicit usings enabled.

## Requirements Summary

Only for `NEW_PROJECT`: compact summary of the requirements document. Write `Not applicable` for
`EXISTING_PROJECT`.

- Not applicable (`EXISTING_PROJECT`)

## Architecture

For `NEW_PROJECT`, this and the Repository Map describe the **planned** structure from the
requirements document until code exists.

- Layering: HTTP controllers -> service layer -> repository interface -> SQL repository/DbContext; DTO, model, and common projects are shared contracts/types.
- Entry points / composition roots: `AlertService.API/Program.cs` composes logging, DI, middleware, health checks, and endpoints.
- User-facing or external interfaces: REST endpoints under `/api/alerts`, Swagger/OpenAPI in development, liveness/readiness health endpoints.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` and related mapping extensions.
- Data / integration boundaries: `AlertService.Data` defines repository contracts; `AlertService.Data.SQL` implements EF Core SQL Server persistence and migration operations.
- Notable dependency flow: API -> Data interfaces -> Data.SQL implementation; service layer depends on abstractions, not EF Core directly.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | API host and composition root | Controllers, middleware, service implementation, and runtime configuration live here. |
| `AlertService.Data/` | Persistence abstraction | Repository interfaces isolate business logic from persistence provider details. |
| `AlertService.Data.SQL/` | SQL Server data provider | DbContext, EF migrations, repository implementation, and DI extensions. |
| `AlertService.DTO/` | Request/response contracts | API contracts used by controllers and service mapping logic. |
| `AlertService.Models/` | Domain entities | Core alert entity definitions used across service/data layers. |
| `AlertService.Common/` | Shared constants/enums | Shared primitives such as severity enum and constraints. |
| `AlertService.API.Tests/` | API/service-focused tests | xUnit/Moq plus ASP.NET Core integration-style health checks. |
| `AlertService.Data.SQL.Tests/` | Data layer tests | Repository behavior tests using EF Core test providers. |
| `database/` | SQL operational scripts | Database creation and idempotent migration scripts. |
| `standards/` | Full standards source | Canonical standards referenced by compact instruction files. |
| `.github/instructions/` | Compact auto-applied instructions | Enforced instruction subsets routed by applyTo patterns. |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` and `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test AlertService.sln`
- Coverage: `dotnet test AlertService.sln --collect:"XPlat Code Coverage"`
- Lint / format: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server (`AlertServiceDb`) via EF Core 8 (`AlertDbContext`).
- Environment / config notes: connection string in `AlertService.API/appsettings.json`; startup migrations toggled by `Database:ApplyMigrationsOnStartup`.
- Operational assets: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`, health endpoints (`/health/live`, `/health/ready`), Serilog rolling files under `Logs/`.

## Testing

- Main test layers: API/controller/service tests plus SQL repository tests.
- Test frameworks: xUnit, Moq, Microsoft.NET.Test.Sdk, ASP.NET Core test host, EF Core InMemory/Sqlite providers.
- Test naming / placement pattern: separate `<Project>.Tests` projects mirroring production concerns.

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Setup, architecture overview, API behavior, and developer commands. |
| `.github/copilot-instructions.md` | SDLC process doc | Shared SDLC stage rules and workflow transitions. |
| `standards/*.md` | Engineering standards | Source-of-truth standards referenced by stage-level instruction routing. |
| `database/01_CreateDatabase.sql` | SQL setup script | Initial database creation script for local/provisioning flows. |
| `database/02_AlertServiceDb_Migrations.sql` | SQL migration artifact | Idempotent EF-generated migration script for DB deployment paths. |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE` until discovered
- ADRs / architecture decision records: `NOT_AVAILABLE` until discovered
- CI/CD pipeline definition: `NOT_AVAILABLE` until discovered
- Deployment runbook: `NOT_AVAILABLE` until discovered
- Separate API contract artifacts (standalone OpenAPI file): `NOT_AVAILABLE` (Swagger is generated at runtime)
