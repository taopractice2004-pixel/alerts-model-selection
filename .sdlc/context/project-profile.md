# Project Profile

<!-- A MAP of the repository, not a copy. Built by /setup-repo-context, refreshed by /refresh-repo-context.
     - Facts only. Coding rules live in standards-summary.md. Build and test commands live in manifest.json.
     - Never list every file, class, or method. Name the important places and one example per pattern.
     - Never store secrets, passwords, tokens, connection strings, or other sensitive values.
     - Sections marked (developer-maintained) are filled by the developer and never overwritten by refresh.
     - GREENFIELD: tag facts [REQ] requirement, [STD] standards, [DEV] developer, [PROPOSED]; use
       NOT_ESTABLISHED for what cannot exist yet and OPEN_DECISION for unsettled decisions. -->

## 0. Repository Mode
- Mode: `EXISTING` (`EXISTING` or `GREENFIELD`)
- Context maturity: `ESTABLISHED` (`INITIAL` or `ESTABLISHED`)
- Requirement source: `NOT_APPLICABLE` (GREENFIELD: path of the requirement document)

## 1. Overview
- Purpose: `AlertService` is a small, enterprise-structured ASP.NET Core Web API for creating, querying, summarizing, deactivating, and deleting operational alerts.
- Main responsibilities: `Expose alert CRUD and summary endpoints; validate API DTOs; apply business rules in a service layer; persist alerts through EF Core to SQL Server; publish health endpoints; log requests and failures with Serilog.`

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | `C#` | `.NET 8 / net8.0` |
| Framework | `ASP.NET Core Web API, Entity Framework Core, Serilog, Swashbuckle` | `ASP.NET Core 8, EF Core 8.0.31, Serilog.AspNetCore 8.0.3, Swashbuckle 6.9.0` |
| Build / package tool | `.NET SDK, NuGet, dotnet-ef local tool` | `.NET 8, dotnet-ef 8.0.31` |
| Data store | `SQL Server via EF Core; SQLite/InMemory in tests` | `SQL Server provider 8.0.31` |
| Test framework | `xUnit, Moq, ASP.NET Core MVC Testing` | `xUnit 2.9.3, Moq 4.20.72, Microsoft.NET.Test.Sdk 17.12.0` |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | API host, controllers, middleware, health endpoints, service implementation, and composition root |
| `AlertService.API.Tests/` | Controller, service, and health-check tests for the API project |
| `AlertService.Common/` | Shared constants and enums reused across layers |
| `AlertService.Data/` | Persistence abstractions such as repository interfaces |
| `AlertService.Data.SQL/` | SQL Server EF Core implementation: `DbContext`, configurations, DI extensions, repository, migrations |
| `AlertService.Data.SQL.Tests/` | Repository tests using EF Core InMemory and SQLite |
| `AlertService.DTO/` | Request and response contracts for the API |
| `AlertService.Models/` | Domain entity classes used by the service and data layers |
| `database/` | SQL scripts for database creation and generated migration deployment script |
| `.sdlc/context/` | Generated repository context consumed by later SDLC stages |
| `.github/instructions/` | Path-scoped instruction files that point tasks to `standards-summary.md` |
| `standards/` | Company standards source documents consumed only during setup/refresh |

## 4. Architecture And Flow
- Style: `Layered microservice-style Web API inside a single solution.`
- Layers and how they interact: `Controller -> IAlertService/AlertManagementService -> IAlertRepository -> AlertDbContext -> SQL Server.`
- Main request / data flow: `ASP.NET Core binds and validates DTOs, controllers delegate to the service layer, the service applies business rules and logging, the repository executes EF Core queries/updates, and mapping extensions convert entities to DTO responses.`
- Entry points and composition root: `AlertService.API/Program.cs` is the HTTP entry point and DI composition root; `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` wires SQL data access and health checks.`
- Architecture rules observed: `Controllers stay thin and never call EF Core directly; service code depends on repository interfaces from AlertService.Data; Data.SQL contains provider-specific persistence details; DTOs stay separate from persistence models; unhandled API exceptions are normalized by middleware.`

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| `AlertService.API` | `AlertService.API/` | Hosts the HTTP API, request pipeline, health endpoints, Swagger, middleware, and alert business service implementation |
| `AlertService.Common` | `AlertService.Common/` | Defines shared constants and enums such as severity and paging/sorting limits |
| `AlertService.DTO` | `AlertService.DTO/` | Defines request and response DTOs plus validation attributes/rules |
| `AlertService.Models` | `AlertService.Models/` | Defines the `Alert` entity model |
| `AlertService.Data` | `AlertService.Data/` | Defines repository abstractions consumed by the service layer |
| `AlertService.Data.SQL` | `AlertService.Data.SQL/` | Implements EF Core persistence, migrations, SQL Server wiring, and readiness health check backing |
| `AlertService.API.Tests` | `AlertService.API.Tests/` | Verifies controller behavior, service rules, and health endpoint responses |
| `AlertService.Data.SQL.Tests` | `AlertService.Data.SQL.Tests/` | Verifies repository filtering, sorting, paging, and aggregate queries |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Attribute-routed controller returning typed `ActionResult` responses |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Business rules, logging, DTO/entity mapping, and repository orchestration |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core query composition, filtering, paging, sorting, and CRUD |
| External integration client | `NOT_APPLICABLE` | No dedicated third-party client layer exists in this repository |
| UI component | `NOT_APPLICABLE` | No frontend project exists in this repository |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | Typical xUnit + Moq test style for service behavior |

## 7. Testing Context
- Test framework: `xUnit 2.9.3 across separate test projects.`
- Mocking and assertion libraries: `Moq 4.20.72 for mocked dependencies; xUnit assertions; ASP.NET Core WebApplicationFactory for API host tests; EF Core InMemory and SQLite for persistence tests.`
- Test location and naming style: `Tests live in sibling projects named *.Tests; files use the *Tests.cs suffix; namespaces mirror the production layer under test.`
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- Coverage tooling: `coverlet.collector is referenced in AlertService.API.Tests; coverage collection is not otherwise centrally configured.`
- Coverage target for new or changed code: `NOT_CONFIGURED`
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `README.md` | Setup, run, migration, and testing instructions |
| `Directory.Build.props` | Shared solution-wide target framework, nullable, and implicit usings settings |
| `dotnet-tools.json` | Local tool manifest for `dotnet-ef` |
| `AlertService.API/appsettings.json` | Default connection string, Serilog sinks/levels, and migration toggle |
| `AlertService.API/appsettings.Development.json` | Development override enabling automatic migrations and debug logging |
| `AlertService.API/Properties/launchSettings.json` | Local launch profile, URLs, and `ASPNETCORE_ENVIRONMENT` |
| `database/01_CreateDatabase.sql` | Manual database creation script |
| `database/02_AlertServiceDb_Migrations.sql` | Generated idempotent migration deployment script |

- Environment variables used (names and purpose only, never values): `ASPNETCORE_ENVIRONMENT` selects environment-specific config; `ConnectionStrings__AlertDb` can override the database connection string; `Database__ApplyMigrationsOnStartup` can toggle automatic migration application; `DOTNET_ROLL_FORWARD` can allow newer runtimes to run the net8.0 projects.`
- Secrets come from: `The repository defaults to local configuration files and supports overriding settings through environment variables; no secret manager or vault integration is configured in-repo.`

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.API runtime code, AlertService.Common, AlertService.Data interfaces, AlertService.Data.SQL hand-written source folders, AlertService.DTO, AlertService.Models, and both *.Tests projects.`
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `README, solution/project/build files, API appsettings and launch settings, database scripts, and generated SDLC context files.`
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `standards/, pipeline skill/template/work folders, .git/, bin/obj, Logs/, and TestResults/.`
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/, EF Core migrations, and the generated idempotent SQL migration script.`

## 10. Domain Glossary (developer-maintained)
Maps story language to code names. 10-20 terms.

| Term | Code name / location |
|---|---|
| `TO_BE_CONFIGURED` | `TO_BE_CONFIGURED` |

## 11. Known Pitfalls (developer-maintained)
- `None yet` (for example: "service X must be running for these tests")

## 12. Story Conventions (developer-maintained)
- Story ID format: `TO_BE_CONFIGURED` (for example `PROJ-123`)
- Acceptance criteria style: `TO_BE_CONFIGURED` (for example Given/When/Then, or a bullet list)

## 13. Shared Files (optional, developer-maintained)
Files several stories often change at the same time. Analysis lists them as stop points in `plan.md`
when a story touches them. Leave as `None` if one developer works on the repository.
- `None`

## 14. Key Documents
| Path | Purpose |
|---|---|
| `README.md` | Primary setup, architecture, API, health-check, and migration guide |
| `standards/coding-standards.md` | Shared code review, reliability, and testing expectations |
| `standards/backend-dotnet-standards.md` | Backend naming, layering, and C# implementation guidance |
| `standards/api-rest-standards.md` | REST contract, routing, status code, and versioning standards |
| `standards/database-standards.md` | Query, migration, and naming expectations for database work |
| `standards/service-architecture-standards.md` | Layering and service-boundary guidance for .NET APIs |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
