# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection (`README.md`, solution/build files, representative source files)

## Overview

- Product: Alert management microservice exposing CRUD, filtering, paging, summary, and health-check endpoints.
- Primary stack: ASP.NET Core Web API on .NET 8 with EF Core 8, SQL Server, Serilog, Swagger, xUnit, and Moq.
- Repository shape: Multi-project .NET solution separating API, DTOs, domain models, data abstractions, SQL data access, and test projects.
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, and implicit usings enabled across projects.

## Requirements Summary

Not applicable for `EXISTING_PROJECT`.

## Architecture

- Layering: Controller layer -> service layer -> repository interface -> SQL data access implementation -> EF Core / SQL Server.
- Entry points / composition roots: `AlertService.API/Program.cs` configures DI, logging, middleware, health endpoints, Swagger, and optional migration execution.
- User-facing or external interfaces (UI/API/CLI/background): REST API under `/api/alerts`, health endpoints under `/health/live` and `/health/ready`, and Swagger UI in development.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` owns alert creation, updates, deactivation, summary mapping, and pagination response assembly.
- Data / integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs` defines the persistence boundary; `AlertService.Data.SQL` implements it with `AlertDbContext`, EF Core configurations, migrations, and SQL Server registration.
- Notable dependency flow: `AlertService.API` depends on DTO, Common, Models, Data abstractions, and Data.SQL only for DI wiring; business logic depends on interfaces rather than EF Core directly.

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP delivery layer and composition root | Contains `Program.cs`, controllers, middleware, health endpoint mapping, DTO/entity mapping, and service implementation. |
| `AlertService.Data/` | Persistence abstractions | Defines repository contracts used by the service layer. |
| `AlertService.Data.SQL/` | SQL Server data access implementation | Holds `AlertDbContext`, EF configuration, migrations, DI registration, and repository implementation. |
| `AlertService.DTO/` | API contracts | Request and response DTOs used by controllers and services. |
| `AlertService.Models/` | Domain entities | Holds the `Alert` entity used across service and persistence layers. |
| `AlertService.Common/` | Shared primitives | Shared enums and constants such as severity and paging/sorting constants. |
| `AlertService.API.Tests/` | API/service tests | xUnit tests for controllers, health checks, and service behavior using Moq and `WebApplicationFactory`. |
| `AlertService.Data.SQL.Tests/` | Persistence tests | xUnit repository tests using EF Core test providers. |
| `database/` | Operational SQL assets | Database creation script and idempotent migration script for manual or DBA-driven setup. |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` (supported by `coverlet.collector` in the API test project)
- Lint / format: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core (`AlertDbContext`).
- Environment / config notes: Connection string `ConnectionStrings:AlertDb` is required; development config can auto-apply migrations when `Database:ApplyMigrationsOnStartup` is `true`.
- Operational assets: Serilog console/file logging, Swagger in development, health checks, EF Core migrations, `database/*.sql` scripts, and local `dotnet-ef` tool manifest.

## Testing

- Main test layers: Controller tests, service tests, health-check integration tests, and repository tests.
- Test frameworks: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory/SQLite helpers, and `coverlet.collector`.
- Test naming / placement pattern: Tests live in parallel `*.Tests` projects with folders mirroring production slices such as `Controllers/`, `Services/`, `Repositories/`, and `TestInfrastructure/`.

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Repository overview, API surface, local setup, EF commands, and architectural notes. |
| `.github/copilot-instructions.md` | SDLC framework doc | Shared pipeline behavior, stage outputs, and repository cache usage rules. |
| `standards/` | Engineering standards set | Source standards for API, backend, database, coding, service architecture, frontend, and UI review rules. |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate OpenAPI contract artifact: `NOT_AVAILABLE` (Swagger is generated from code; no checked-in OpenAPI document found)
