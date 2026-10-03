# Project Profile

<!-- A MAP of the repository, not a copy. Built by /setup-repo-context, refreshed by /refresh-repo-context.
     - Facts only. Coding rules live in standards-summary.md. Build and test commands live in manifest.json.
     - Never list every file, class, or method. Name the important places and one example per pattern.
     - Never store secrets, passwords, tokens, connection strings, or other sensitive values.
     - Sections marked (developer-maintained) are filled by the developer and never overwritten by refresh.
     - GREENFIELD: tag facts [REQ] requirement, [STD] standards, [DEV] developer, [PROPOSED]; use
       NOT_ESTABLISHED for what cannot exist yet and OPEN_DECISION for unsettled decisions. -->

## 0. Repository Mode
- Mode: `EXISTING`
- Context maturity: `ESTABLISHED`
- Requirement source: `NOT_APPLICABLE`

## 1. Overview
- Purpose: AlertService, a small ASP.NET Core Web API (.NET 8) microservice for managing alerts.
- Main responsibilities: alert CRUD and deactivate; paged, filtered, sorted, title-searchable listing; aggregate summary counts by status and severity; liveness and readiness health endpoints.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# (nullable and implicit usings enabled) | .NET 8 (`net8.0`) |
| Framework | ASP.NET Core Web API (controllers), Swashbuckle (Swagger), Serilog | 8.x; Swashbuckle 6.9.0; Serilog.AspNetCore 8.0.3 |
| Build / package tool | .NET SDK, `AlertService.sln`, NuGet, shared `Directory.Build.props`, local tool `dotnet-ef` | dotnet-ef 8.0.31 |
| Data store | SQL Server via EF Core (`Microsoft.EntityFrameworkCore.SqlServer`) | 8.0.31 |
| Test framework | xUnit, Moq, EF Core InMemory and SQLite, `Microsoft.AspNetCore.Mvc.Testing`, coverlet.collector | xUnit 2.9.3; Moq 4.20.72 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | HTTP layer, business services, DI composition root (`Program.cs`) |
| `AlertService.API/Controllers/` | Thin controllers |
| `AlertService.API/Services/` | Business logic (`IAlertService`, `AlertManagementService`) |
| `AlertService.API/Mappings/` | Entity <-> DTO mapping extension methods |
| `AlertService.API/Middleware/` | Exception handling middleware (500 ProblemDetails) |
| `AlertService.API/Extensions/` | Health check endpoint mapping |
| `AlertService.Common/` | Shared enums and constants (`Severity`, `AlertConstants`) |
| `AlertService.DTO/` | Request and response contracts (`Requests/`, `Responses/`) |
| `AlertService.Models/` | Domain entities (`Alert`) |
| `AlertService.Data/` | Data-access abstractions (`Interfaces/IAlertRepository`) |
| `AlertService.Data.SQL/` | EF Core implementation: `AlertDbContext`, `Configurations/`, `Repositories/`, `Extensions/`, `Migrations/` |
| `AlertService.API.Tests/` | Controller, service, and health check tests |
| `AlertService.Data.SQL.Tests/` | Repository tests |
| `database/` | SQL scripts: create database, idempotent migration script |
| `standards/` | Company standards documents (read only by setup and refresh) |

## 4. Architecture And Flow
- Style: layered service with repository pattern and a service layer; one project per layer.
- Layers and how they interact: Controller -> `IAlertService` (`AlertManagementService`) -> `IAlertRepository` (`AlertRepository`) -> `AlertDbContext` -> SQL Server. Project dependencies: API -> DTO -> Common; API -> Data (interfaces) -> Models -> Common; API -> Data.SQL -> Data.
- Main request / data flow: HTTP request -> `ExceptionHandlingMiddleware` -> `AlertsController` (DataAnnotations validation on DTOs via `[ApiController]`) -> service (maps via `AlertMappingExtensions`, stamps `CreatedDate` from `TimeProvider`, logs) -> repository (the only EF Core user) -> response DTO.
- Entry points and composition root: `AlertService.API/Program.cs` (controllers, Swagger, `AddSqlDataAccess`, `AddAlertHealthChecks`, `TimeProvider.System`, scoped `IAlertService`); health endpoints via `MapAlertHealthEndpoints`; `public partial class Program` is exposed for `WebApplicationFactory`.
- Architecture rules observed: controllers call only `IAlertService`; the service depends only on `IAlertRepository`; the API references `Data.SQL` only to register it in DI; "not found" is `null`/`false` from the service and 404 in the controller; DTOs are separate from entities; enums serialize as strings; paged repository queries return `(Items, TotalCount)`.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API | `AlertService.API/` | Controllers, services, mapping, middleware, startup, health endpoints |
| Common | `AlertService.Common/` | `Severity` enum, `AlertConstants` (limits, sort keys, regex patterns) |
| DTO | `AlertService.DTO/` | Alert request and response contracts, `PagedResponse<T>` |
| Models | `AlertService.Models/` | `Alert` entity |
| Data | `AlertService.Data/` | `IAlertRepository` contract |
| Data.SQL | `AlertService.Data.SQL/` | SQL Server persistence, EF configuration, migrations, DI and migration helpers |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | `[ApiController]`, `[Route("api/alerts")]`, `ProducesResponseType`, `CancellationToken` on every action, `null` -> `NotFound()` |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Interface in `IAlertService.cs`; registered scoped in `Program.cs` |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | Interface in `AlertService.Data/Interfaces/IAlertRepository.cs`; `AsNoTracking` for reads; entity mapping in `Configurations/AlertConfiguration.cs` |
| Request / response DTO | `AlertService.DTO/Requests/CreateAlertRequest.cs` | DataAnnotations using `AlertConstants` |
| DI registration extension | `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` | `AddSqlDataAccess`, `AddAlertHealthChecks`, `ApplyMigrationsAsync` |
| External integration client | `NOT_APPLICABLE` | No external calls exist |
| UI component | `NOT_APPLICABLE` | No frontend |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | Moq repository, fixed `TimeProvider`, `NullLogger` |

## 7. Testing Context
- Test framework: xUnit 2.9.3 (`[Fact]`); `Xunit` is a global using in the test projects.
- Mocking and assertion libraries: Moq 4.20.72; xUnit `Assert` (no assertion library). EF Core InMemory (repository tests) and SQLite in-memory (health check tests).
- Test location and naming style: one test project per layer; folders mirror the code under test (`Controllers/`, `Services/`, `Repositories/`, `TestInfrastructure/`); class `<Type>Tests`; methods `Method_Scenario[_Expected]`, for example `AddAsync_PersistsAlert_AndAssignsId`.
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`; repository: `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`; HTTP-level: `AlertService.API.Tests/HealthChecksTests.cs` with `TestInfrastructure/HealthChecksWebApplicationFactory.cs`.
- Coverage tooling: `coverlet.collector` 6.0.2, referenced only in `AlertService.API.Tests`.
- Coverage target for new or changed code: 90% per company `standards/coding-standards.md` ("when that project rule applies"); no threshold is configured in the repository.
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | `ConnectionStrings:AlertDb`, `Database:ApplyMigrationsOnStartup` (false), Serilog sinks (console, rolling file under `Logs/`), `AllowedHosts` |
| `AlertService.API/appsettings.Development.json` | Development overrides: `ApplyMigrationsOnStartup` true, verbose logging |
| `AlertService.API/Properties/launchSettings.json` | Local launch profiles (`https`; Swagger UI) |
| `Directory.Build.props` | Shared `net8.0`, nullable, implicit usings |
| `dotnet-tools.json` | Local `dotnet-ef` tool manifest |
| `AlertService.API/AlertService.API.http` | Manual HTTP requests for the API |

- Environment variables used (names and purpose only, never values): `DOTNET_ROLL_FORWARD` (README: run on a newer runtime than .NET 8); standard ASP.NET Core overrides such as `ConnectionStrings__AlertDb` apply. No custom variables.
- Secrets come from: `TO_BE_DISCOVERED` (the local connection string lives in `appsettings.json`; no vault or user-secrets setup is documented)

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): the six `AlertService.*` source projects and the two `*.Tests` projects.
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `AlertService.sln`, `*.csproj`, `Directory.Build.props`, `dotnet-tools.json`, `appsettings*.json`, `Properties/`, `*.http`, `database/`, `README.md`.
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `bin/`, `obj/`, `.vs/`, `Logs/`, `TestResults/`, `.git/`, `standards/`, pipeline files (`.github/skills/`, `.sdlc/templates/`, `.sdlc/work/`).
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/`, `AlertService.Data.SQL/Migrations/` (EF-generated), `database/02_AlertServiceDb_Migrations.sql` (generated script).

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
| `README.md` | Structure, dependency flow, API contract, health checks, run and EF migration commands, design notes |
| `standards/` | Company standards (backend .NET, REST API, database, coding, service architecture); condensed in `standards-summary.md` |
| `database/` | SQL scripts: database creation and the idempotent migration script |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
