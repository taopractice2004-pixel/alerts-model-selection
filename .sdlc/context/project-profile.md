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
- Purpose: Manage alert records through a REST API with filtering, paging, and summary endpoints, backed by SQL Server and EF Core.
- Main responsibilities: HTTP request handling, alert business rules, persistence through repository abstractions, health/readiness checks, and migration-aware startup.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# | .NET 8 (`net8.0`) |
| Framework | ASP.NET Core Web API, EF Core, Serilog | ASP.NET Core 8, EF Core 8.0.31, Serilog.AspNetCore 8.0.3 |
| Build / package tool | dotnet CLI + NuGet + local `dotnet-ef` tool | SDK-style projects, `dotnet-ef` 8.0.31 |
| Data store | SQL Server | EF Core SQL Server provider 8.0.31 |
| Test framework | xUnit + Moq (+ ASP.NET Core test host) | xUnit 2.9.3, Moq 4.20.72, Microsoft.NET.Test.Sdk 17.12.0 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | API host, controllers, middleware, service implementations, DI composition root |
| `AlertService.Data/` | Data access contracts (interfaces) consumed by API layer |
| `AlertService.Data.SQL/` | EF Core SQL Server implementation: DbContext, repository, migrations, DI extensions |
| `AlertService.Models/` | Domain entity types used by data and service layers |
| `AlertService.DTO/` | API request/response contracts and validation attributes |
| `AlertService.Common/` | Shared enums and constants |
| `AlertService.API.Tests/` | API/service/controller/health-check automated tests |
| `AlertService.Data.SQL.Tests/` | Repository/data-access tests with EF in-memory and SQLite |
| `database/` | SQL bootstrap and idempotent migration scripts |
| `.sdlc/context/` | Generated repository context artifacts consumed by SDLC pipeline stages |
| `.github/instructions/` | Path-based instruction routers to `standards-summary.md` sections |
| `standards/` | Source standards documents (condensed into `.sdlc/context/standards-summary.md`) |

## 4. Architecture And Flow
- Style: Layered .NET service (microservice-shaped API with explicit DTO/service/repository boundaries).
- Layers and how they interact: Controller -> `IAlertService`/`AlertManagementService` -> `IAlertRepository` -> `AlertRepository`/`AlertDbContext` -> SQL Server.
- Main request / data flow: Request DTO binding and validation (`[ApiController]`) -> service business rules/mapping/logging -> repository query or mutation -> EF Core persistence -> response DTO mapping.
- Entry points and composition root: `AlertService.API/Program.cs` configures logging, DI, middleware, health endpoints, controllers, and optional migration-on-startup.
- Architecture rules observed: Controllers remain thin; services never depend directly on EF Core; repository abstraction is injected; global exception middleware returns RFC7807-like `ProblemDetails`; async APIs propagate `CancellationToken`.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API host | `AlertService.API/` | HTTP routing, serialization, middleware pipeline, DI wiring |
| Alert application service | `AlertService.API/Services/` | Alert use cases, business rule enforcement, DTO mapping orchestration |
| Data contracts | `AlertService.Data/Interfaces/` | Persistence abstractions used by service layer |
| SQL data access | `AlertService.Data.SQL/` | EF Core model, queries, persistence operations, migration definitions |
| Contracts | `AlertService.DTO/` | Input/output models and validation constraints |
| Domain model | `AlertService.Models/` | Alert entity state model |
| Shared primitives | `AlertService.Common/` | Cross-layer constants and severity enum |
| Automated tests | `AlertService.API.Tests/`, `AlertService.Data.SQL.Tests/` | Unit-style and component-level verification for API/service/repository behavior |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Route attributes, status-code mapping, typed responses |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Business rules, logging, request-to-entity mapping, null/not-found handling |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core querying, filters, paging, sorting, summary aggregation |
| External integration client | `NOT_APPLICABLE` | No outbound third-party client adapter in current codebase |
| UI component | `NOT_APPLICABLE` | No frontend/UI project in this repository |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | xUnit + Moq, AAA style, behavior-focused method names |

## 7. Testing Context
- Test framework: xUnit (`Fact`, `Theory`) via `Microsoft.NET.Test.Sdk`.
- Mocking and assertion libraries: Moq; xUnit assertions.
- Test location and naming style: Dedicated test projects (`AlertService.API.Tests`, `AlertService.Data.SQL.Tests`); class names end with `Tests`; methods follow `MethodName_Condition_ExpectedBehavior` style.
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`.
- Coverage tooling: `coverlet.collector` in `AlertService.API.Tests`; no repository-wide coverage pipeline found.
- Coverage target for new or changed code: `NOT_CONFIGURED`.
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Base runtime configuration, SQL connection string key, Serilog sinks/levels |
| `AlertService.API/appsettings.Development.json` | Development overrides (debug logging and startup migration toggle) |
| `AlertService.API/Properties/launchSettings.json` | Local launch profiles and environment setup |
| `Directory.Build.props` | Shared repository-wide .NET build properties (`net8.0`, nullable, implicit usings) |
| `dotnet-tools.json` | Local CLI tool manifest (`dotnet-ef`) |

- Environment variables used (names and purpose only, never values): `ConnectionStrings__AlertDb` (database connection override), `Database__ApplyMigrationsOnStartup` (migration toggle), `ASPNETCORE_ENVIRONMENT` (environment selection), `DOTNET_ROLL_FORWARD` (runtime roll-forward behavior for local execution).
- Secrets come from: environment-variable overrides and/or local/deployment secret stores; connection string values are not committed as secrets in context artifacts.

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.API/**`, `AlertService.Data/**`, `AlertService.Data.SQL/**`, `AlertService.Common/**`, `AlertService.DTO/**`, `AlertService.Models/**`, `AlertService.API.Tests/**`, `AlertService.Data.SQL.Tests/**`.
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): solution/project/build files, appsettings/launch settings, `database/**`, `.sdlc/context/**`, and `.github/instructions/**`.
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `**/bin/**`, `**/obj/**`, `.vs/**`, `Logs/**`, `TestResults/**`, pipeline skill/template/work state paths.
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/**` plus EF generated migration snapshot/designer files.

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
| `README.md` | Setup, architecture overview, API behavior, and developer commands |
| `database/02_AlertServiceDb_Migrations.sql` | Idempotent migration SQL for database deployments |
| `.github/copilot-instructions.md` | Repository-level Copilot operating rules and SDLC-stage contracts |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
