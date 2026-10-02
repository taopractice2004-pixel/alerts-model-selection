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
- Purpose: ASP.NET Core Web API microservice for creating, querying, updating, deactivating, deleting, and summarizing operational alerts.
- Main responsibilities: expose REST endpoints, enforce alert business rules in service layer, persist alert data through EF Core SQL Server repository, provide health endpoints, and emit structured logs.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# | .NET 8 (`net8.0`) |
| Framework | ASP.NET Core Web API, Entity Framework Core, Serilog, Swagger (Swashbuckle) | ASP.NET Core 8 / EF Core 8.0.31 |
| Build / package tool | dotnet CLI, SDK-style csproj, NuGet, local dotnet tool manifest | .NET SDK 8+, `dotnet-ef` 8.0.31 |
| Data store | SQL Server via EF Core (`AlertDbContext`) | SQL Server provider 8.0.31 |
| Test framework | xUnit, Moq, ASP.NET Core `WebApplicationFactory`, EF Core InMemory/Sqlite, coverlet collector | xUnit 2.9.3, Moq 4.20.72, coverlet.collector 6.0.2 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | API host, controllers, middleware, DI composition root, health endpoint mapping, service implementations |
| `AlertService.Common/` | Shared constants and enums used across modules |
| `AlertService.DTO/` | Request and response contracts for API boundary |
| `AlertService.Models/` | Domain entity models |
| `AlertService.Data/` | Data-access abstractions and repository interfaces |
| `AlertService.Data.SQL/` | EF Core SQL Server implementation (`DbContext`, repository, migrations, DI extensions) |
| `AlertService.API.Tests/` | API and service unit tests, health endpoint integration-style tests with test host |
| `AlertService.Data.SQL.Tests/` | Repository/data-access tests against InMemory and Sqlite providers |
| `database/` | SQL scripts for database creation and migration deployment |
| `standards/` | Team/company standards documents (source material for standards summary) |
| `.sdlc/context/` | Cached repository context artifacts used by pipeline stages |
| `.github/instructions/` | Path-based instruction entry points pointing to standards summary |

## 4. Architecture And Flow
- Style: Layered service-oriented Web API (single service repository with modular projects)
- Layers and how they interact: Controller -> Service (`IAlertService`) -> Repository (`IAlertRepository`) -> EF Core `AlertDbContext` -> SQL Server
- Main request / data flow: ASP.NET Core endpoint receives request DTO -> model binding/validation -> controller delegates to service -> service applies business rules and mapping -> repository executes EF Core query/command -> service maps to response DTO -> controller returns HTTP status/result.
- Entry points and composition root: `AlertService.API/Program.cs` configures host, middleware, DI, logging, Swagger, health endpoints, and optional startup migration execution.
- Architecture rules observed: controllers stay thin; business logic resides in service layer; repositories encapsulate persistence; DTOs are separate from entity models; dependencies are registered in DI extensions.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API Host | `AlertService.API/` | HTTP routing, middleware pipeline, service registrations, health endpoints |
| Business Services | `AlertService.API/Services/` | Alert business logic, orchestration, logging, mapping use |
| Data Abstractions | `AlertService.Data/` | Repository contracts used by services |
| SQL Data Access | `AlertService.Data.SQL/` | EF Core context/configuration/repositories/migrations and SQL wiring |
| Contracts | `AlertService.DTO/` | API request/response models and query contract |
| Domain Models | `AlertService.Models/` | Alert entity model |
| Shared Types | `AlertService.Common/` | Cross-module constants and enums |
| Test Suites | `AlertService.API.Tests/`, `AlertService.Data.SQL.Tests/` | Verification of API/service behavior and repository behavior |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Thin controller with typed responses and route attributes |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Business logic, mapping, logging, repository orchestration |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core filtering/sorting/paging and aggregate summary query |
| External integration client | `NOT_APPLICABLE` | No outbound third-party HTTP/gRPC client layer present |
| UI component | `NOT_APPLICABLE` | No frontend application in this repository |
| Unit test | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` | xUnit + Moq AAA tests with request validation cases |

## 7. Testing Context
- Test framework: xUnit across test projects
- Mocking and assertion libraries: Moq for dependency mocking; xUnit assertions; ASP.NET Core test host (`Microsoft.AspNetCore.Mvc.Testing`) for endpoint tests
- Test location and naming style: project-level test assemblies `AlertService.API.Tests/` and `AlertService.Data.SQL.Tests/`; test classes named `*Tests`; test methods use descriptive `Action_Condition_Outcome` style
- Representative test file: `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`
- Coverage tooling: `coverlet.collector` package is configured in API test project
- Coverage target for new or changed code: `NOT_CONFIGURED`
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Base runtime configuration: connection string, migration flag, Serilog sinks/levels, host settings |
| `AlertService.API/appsettings.Development.json` | Development overrides (startup migration toggle and log levels) |
| `AlertService.API/Properties/launchSettings.json` | Local launch profiles for API host |
| `Directory.Build.props` | Shared build properties for all projects (`net8.0`, nullable, implicit usings) |
| `dotnet-tools.json` | Local tool manifest (`dotnet-ef`) |
| `database/01_CreateDatabase.sql` | SQL script for initial database creation |
| `database/02_AlertServiceDb_Migrations.sql` | Idempotent SQL migration script generated from EF migrations |

- Environment variables used (names and purpose only, never values): `ConnectionStrings__AlertDb` (database connection), `Database__ApplyMigrationsOnStartup` (startup migration behavior), `ASPNETCORE_ENVIRONMENT` (environment selection), `DOTNET_ROLL_FORWARD` (runtime compatibility for newer installed runtimes)
- Secrets come from: connection details are expected from environment-specific config or environment variables; no secret values are stored in context artifacts.

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.API/`, `AlertService.Common/`, `AlertService.DTO/`, `AlertService.Models/`, `AlertService.Data/`, `AlertService.Data.SQL/`, `AlertService.API.Tests/`, `AlertService.Data.SQL.Tests/`
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): solution/build/config files, `.sdlc/context/`, SQL deployment scripts, and README
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `bin/`, `obj/`, `.git/`, `.vs/`, test output folders, and pipeline prompt/skill/template/work files
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/`, generated migration script file, and migration snapshots/history files

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
| `README.md` | Primary repository overview, architecture summary, local run steps, API behavior, and EF commands |
| `standards/coding-standards.md` | Cross-team coding/review expectations |
| `standards/backend-dotnet-standards.md` | C# and backend coding conventions |
| `standards/api-rest-standards.md` | REST contract, resource design, and versioning guidance |
| `standards/database-standards.md` | SQL and schema naming/query expectations |
| `standards/service-architecture-standards.md` | Layering and microservice architecture guidance |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
