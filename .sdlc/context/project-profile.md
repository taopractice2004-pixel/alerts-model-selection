# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection from existing code and docs

## Overview

- Product: Alert management microservice exposing CRUD, query, summary, and health-check endpoints.
- Primary stack: ASP.NET Core Web API on .NET 8 with EF Core, SQL Server, Serilog, and Swagger.
- Repository shape: Multi-project solution split into API, DTO, domain/shared libraries, data abstractions, SQL implementation, and test projects.
- Shared build/runtime settings: `net8.0`, nullable enabled, implicit usings enabled, warnings not treated as errors.

## Requirements Summary

Not applicable for `EXISTING_PROJECT`.

## Architecture

- Layering: Controller -> service -> repository interface -> EF Core SQL repository, with DTO and domain model separation.
- Entry points / composition roots: `AlertService.API/Program.cs` configures DI, logging, middleware, Swagger, migrations, health checks, and controllers.
- User-facing or external interfaces: REST API under `/api/alerts`, Swagger in Development, and `/health/live` plus `/health/ready` probes.
- Business logic / orchestration locations: `AlertService.API/Services/AlertManagementService.cs` owns alert rules, mapping, and logging.
- Data / integration boundaries: `AlertService.Data/Interfaces` defines repository contracts; `AlertService.Data.SQL` owns `AlertDbContext`, SQL Server wiring, migrations, and repository implementation.
- Notable dependency flow: API depends on DTO, Common, Models, Data abstractions, and Data.SQL only for DI registration; tests target API and Data.SQL separately.

## Repository Map

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | Web API host and composition root | Main HTTP surface, middleware, mappings, controllers, and service implementation. |
| `AlertService.Data/` | Data-access abstractions | Narrow repository interfaces used by the service layer. |
| `AlertService.Data.SQL/` | SQL Server persistence implementation | EF Core DbContext, migrations, health-check registration, and repository code. |
| `AlertService.DTO/` | Request and response contracts | API-facing models and paging/summary response types. |
| `AlertService.Models/` | Domain entities | Core alert entity shared by service and repository layers. |
| `AlertService.Common/` | Shared enums and constants | Severity enum and query/paging constants. |
| `AlertService.API.Tests/` | API and service tests | xUnit tests with Moq and ASP.NET Core test host support. |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit tests using EF Core test providers for persistence behavior. |
| `database/` | SQL operational scripts | Database creation and idempotent migration script outputs. |
| `.github/instructions/` | Compact standards routing | Auto-applied instructions used by later SDLC stages. |
| `standards/` | Full standards source docs | Detailed standards referenced only when an exact rule is needed. |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` and `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` via `coverlet.collector`
- Lint / format: `NOT_CONFIGURED`
- Static analysis / code metrics, per stack — only tools defined in the repository (analyzer packages / `.editorconfig` rules, ESLint config + `devDependency`), never global tools or IDE extensions; used by `/code-review`: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server through EF Core with migrations in `AlertService.Data.SQL/Migrations/`.
- Environment / config notes: Connection string key is `ConnectionStrings:AlertDb`; Development can auto-apply migrations through `Database:ApplyMigrationsOnStartup`.
- Operational assets: `database/*.sql`, `AlertService.API/AlertService.API.http`, Swagger UI, and health endpoints.

## Testing

- Main test layers: Controller/service unit tests, repository tests, and lightweight health/integration coverage.
- Test frameworks: xUnit, Moq, ASP.NET Core MVC Testing, EF Core InMemory, and SQLite test providers.
- Test naming / placement pattern: Tests live in dedicated `*.Tests` projects with feature-area subfolders mirroring production code.

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Service overview, architecture, API surface, local setup, and migration commands. |
| `database/01_CreateDatabase.sql` | Database script | Creates the AlertService database before applying migrations when needed. |
| `database/02_AlertServiceDb_Migrations.sql` | Database script | Idempotent schema script generated from EF Core migrations. |
| `standards/` | Standards docs | Source standards for coding, review, API, database, and service architecture guidance. |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate API contract artifacts: `NOT_AVAILABLE` beyond runtime Swagger generation
