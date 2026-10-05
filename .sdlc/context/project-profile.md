# Project Profile

> Human-readable repository facts. Machine-readable routing (repository mode, detected
> technologies, exclusions, standards index) lives in `manifest.json`; do not repeat it here.
> `/setup-repo-context` overwrites this starter; `/refresh-repo-context` patches stale sections.
> Stages read this file only for a missing fact recorded in `work.json`.

- Source: repository inspection of `README.md`, solution/project files, and representative source files

## Overview

- Product: Alert management microservice for CRUD, filtering, summaries, and health reporting over HTTP
- Primary stack: ASP.NET Core Web API on .NET 8, EF Core 8, SQL Server, Serilog, Swagger, xUnit, Moq
- Repository shape: multi-project solution with API, DTO/common/model libraries, data abstraction, SQL implementation, and separate test projects
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, and implicit usings enabled for all projects

## Requirements Summary

Only for `NEW_PROJECT`: compact summary of the requirements document — key features, external
interfaces, data, and non-functional constraints. Write `Not applicable` for `EXISTING_PROJECT`.

- Not applicable

## Architecture

For `NEW_PROJECT`, this and the Repository Map describe the **planned** structure from the
requirements document until code exists.

- Layering: controller layer in `AlertService.API`, service/orchestration layer in `AlertService.API/Services`, repository contract in `AlertService.Data`, SQL persistence in `AlertService.Data.SQL`, contracts in `AlertService.DTO`, domain entity in `AlertService.Models`, shared enums/constants in `AlertService.Common`
- Entry points / composition roots: `AlertService.API/Program.cs` configures DI, logging, health checks, middleware, Swagger, and optional startup migrations
- User-facing or external interfaces (UI/API/CLI/background): REST API under `/api/alerts`, health endpoints under `/health/live` and `/health/ready`, Swagger UI in Development, and EF CLI/database scripts for schema management
- Business logic / orchestration locations: `AlertManagementService` handles request validation boundaries, mapping, paging results, timestamps, update/deactivate/delete flows, and logging
- Data / integration boundaries: `IAlertRepository` isolates data access from the service layer; `AlertDbContext` and provider-specific repository code own EF Core and SQL Server interaction
- Notable dependency flow: API -> DTO/Common/Data abstractions -> Models/Common, with API referencing `Data.SQL` only at the composition root for registration

## Repository Map

Important areas and control points only — not a full directory listing.

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP entry point and business services | Main composition root; includes controllers, middleware, mappings, health endpoints, and service implementations |
| `AlertService.Data/` | Persistence abstractions | Holds `IAlertRepository`, the seam most stories and tests should preserve |
| `AlertService.Data.SQL/` | SQL Server persistence implementation | Contains `AlertDbContext`, repository implementation, EF configuration, extension methods, and migrations |
| `AlertService.DTO/` | API request/response contracts | External contract surface for controller and service changes |
| `AlertService.Models/` | Domain entities | Shared persistence/business shape for alerts |
| `AlertService.Common/` | Shared enums and constants | Cross-project primitives such as `Severity` and field constraints |
| `AlertService.API.Tests/` | API/service tests | xUnit tests for controllers, services, and health checks using Moq and `WebApplicationFactory` |
| `AlertService.Data.SQL.Tests/` | Persistence tests | xUnit tests for repository behavior using EF Core test providers |
| `database/` | Operational SQL artifacts | Database creation script and idempotent migration script for SQL deployments |

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Coverage: `TO_BE_DISCOVERED` (test projects reference `coverlet.collector`, but no repository-level coverage command or threshold is documented)
- Lint / format: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server accessed through EF Core 8
- Environment / config notes: connection string `ConnectionStrings:AlertDb` lives in `AlertService.API/appsettings*.json`; `Database:ApplyMigrationsOnStartup` controls startup migrations in Development
- Operational assets: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`, EF CLI tooling via `dotnet-ef`, Serilog console/file logging, and liveness/readiness endpoints

## Testing

- Main test layers: API/controller/service tests in `AlertService.API.Tests` and persistence tests in `AlertService.Data.SQL.Tests`
- Test frameworks: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory/SQLite helpers, and `Microsoft.NET.Test.Sdk`
- Test naming / placement pattern: dedicated `*.Tests` projects with test classes named `*Tests.cs` mirroring the production slice

## Project Docs

Index only — never copy document bodies here.

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Repository overview, API behavior, local run steps, health checks, and EF migration commands |
| `AlertService.API/AlertService.API.http` | HTTP request artifact | Manual API smoke requests for local development |
| `database/01_CreateDatabase.sql` | Database setup script | Creates the target SQL Server database |
| `database/02_AlertServiceDb_Migrations.sql` | Database migration artifact | Idempotent SQL generated from EF migrations for DBA or CI-driven schema updates |
| `standards/api-rest-standards.md` | Engineering standard | REST contract guidance for controller and API surface changes |
| `standards/backend-dotnet-standards.md` | Engineering standard | C# and backend layering guidance for .NET code |
| `standards/coding-standards.md` | Engineering standard | Global coding/review expectations |
| `standards/database-standards.md` | Engineering standard | Database, migration, and query guidance |
| `standards/service-architecture-standards.md` | Engineering standard | Service boundary and operational guidance |

## Known Gaps

- BRDs / formal requirements: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate OpenAPI source file: `NOT_AVAILABLE` (Swagger is generated at runtime; no checked-in OpenAPI document was found)
