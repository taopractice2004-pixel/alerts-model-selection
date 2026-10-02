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
- Purpose: AlertService, an ASP.NET Core Web API microservice for managing alerts (create, read, update, deactivate, delete, summary).
- Main responsibilities: alert CRUD with filtering, paging, sorting, and title search; aggregate summary by status and severity; liveness and readiness health probes.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# (nullable and implicit usings enabled) | .NET 8 (`net8.0`) |
| Framework | ASP.NET Core Web API (controllers), Swashbuckle (Swagger), Serilog | Serilog.AspNetCore 8.0.3, Swashbuckle 6.9.0 |
| Build / package tool | .NET SDK, solution `AlertService.sln`, shared `Directory.Build.props`, NuGet; local tool `dotnet-ef` | dotnet-ef 8.0.31 |
| Data store | SQL Server via EF Core (`UseSqlServer`, migrations) | EF Core 8.0.31 |
| Test framework | xUnit | 2.9.3 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | HTTP layer, business services, composition root (`Program.cs`) |
| `AlertService.API/Controllers/` | Thin controllers (`AlertsController`) |
| `AlertService.API/Services/` | Business logic (`IAlertService`, `AlertManagementService`) |
| `AlertService.API/Mappings/` | Entity <-> DTO mapping extensions |
| `AlertService.API/Middleware/` | `ExceptionHandlingMiddleware` (unhandled errors -> 500 ProblemDetails) |
| `AlertService.API/Extensions/` | Endpoint extensions (health check endpoints) |
| `AlertService.Common/` | Shared enums and constants (`Severity`, `AlertConstants`) |
| `AlertService.DTO/` | Request and response contracts (`Requests/`, `Responses/`) |
| `AlertService.Models/` | Domain entities (`Alert`) |
| `AlertService.Data/` | Data-access abstractions (`Interfaces/IAlertRepository`) |
| `AlertService.Data.SQL/` | SQL Server implementation: `AlertDbContext`, `Configurations/`, `Repositories/`, `Migrations/`, `Extensions/` |
| `AlertService.API.Tests/` | Controller, service, and health-check tests |
| `AlertService.Data.SQL.Tests/` | Repository tests |
| `database/` | `01_CreateDatabase.sql`, idempotent script generated from EF migrations |
| `standards/` | Company standards documents (setup and refresh only) |

## 4. Architecture And Flow
- Style: layered service (API -> service -> repository -> SQL Server) split into one project per layer.
- Layers and how they interact: Controller -> `IAlertService` -> `IAlertRepository` -> `AlertDbContext` (EF Core). Project flow: API -> DTO -> Common; API -> Data (interfaces) -> Models -> Common; API -> Data.SQL -> Data.
- Main request / data flow: `AlertsController` receives a DTO request and calls `AlertManagementService`, which calls `IAlertRepository`; entities are mapped to response DTOs by `AlertMappingExtensions`; the collection endpoint returns `PagedResponse<AlertResponse>`.
- Entry points and composition root: `AlertService.API/Program.cs` (DI, Serilog, middleware, Swagger in Development, optional startup migrations via `Database:ApplyMigrationsOnStartup`); `AddSqlDataAccess` in `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` registers the DbContext and repository.
- Architecture rules observed: controllers call only the service; the service depends only on `IAlertRepository`; API references `Data.SQL` only to register DI; DTOs are separate from entities; page sizes, sort fields, and lengths live in `AlertConstants`; `TimeProvider` is injected for time; unhandled exceptions become RFC 7807 `ProblemDetails`; enums serialize as strings.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API | `AlertService.API/` | Controllers, services, mapping, middleware, health endpoints, host setup |
| Common | `AlertService.Common/` | `Severity` enum, `AlertConstants` |
| DTO | `AlertService.DTO/` | Create, update, and query requests; alert, paged, and summary responses |
| Models | `AlertService.Models/` | `Alert` entity |
| Data | `AlertService.Data/` | `IAlertRepository` |
| Data.SQL | `AlertService.Data.SQL/` | `AlertDbContext`, `AlertConfiguration`, `AlertRepository`, EF migrations, DI and health-check registration |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | `[ApiController]`, route `api/alerts`, `ProducesResponseType`, `CancellationToken`, XML summary per action |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` (contract `IAlertService.cs`) | Constructor DI of repository, `TimeProvider`, `ILogger`; registered scoped in `Program.cs` |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` (contract `AlertService.Data/Interfaces/IAlertRepository.cs`) | `AsNoTracking` reads, filtering in the database, paging |
| Entity configuration / migration | `AlertService.Data.SQL/Configurations/AlertConfiguration.cs` | `IEntityTypeConfiguration<T>`; migrations in `AlertService.Data.SQL/Migrations/` |
| Request / response DTO | `AlertService.DTO/Requests/CreateAlertRequest.cs` | DataAnnotations validation using `AlertConstants` |
| External integration client | `NOT_APPLICABLE` | No external clients |
| UI component | `NOT_APPLICABLE` | No frontend |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | Moq repository, fixed `TimeProvider`, `NullLogger` |

## 7. Testing Context
- Test framework: xUnit 2.9.3 (`Xunit` global using in the test projects).
- Mocking and assertion libraries: Moq 4.20.72; xUnit `Assert` (no separate assertion library); `Microsoft.AspNetCore.Mvc.Testing` for web host tests; EF Core InMemory and Sqlite for data tests.
- Test location and naming style: separate test projects `AlertService.API.Tests` and `AlertService.Data.SQL.Tests`; folders mirror the source (`Controllers/`, `Services/`, `Repositories/`, `TestInfrastructure/`); files `<Class>Tests.cs`; methods `Method_Behavior` (for example `GetAllAsync_MapsEntitiesToPagedResponse`); `[Fact]`.
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`; repository: `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`; health checks: `AlertService.API.Tests/HealthChecksTests.cs`.
- Coverage tooling: `coverlet.collector` 6.0.2 (in `AlertService.API.Tests` only).
- Coverage target for new or changed code: 90% (company coding standard, where the project rule applies); no threshold is enforced in the repository.
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Connection string section, `Database:ApplyMigrationsOnStartup`, Serilog sinks and levels |
| `AlertService.API/appsettings.Development.json` | Development overrides |
| `AlertService.API/Properties/launchSettings.json` | Local launch profiles |
| `AlertService.API/AlertService.API.http` | Manual HTTP request samples |
| `Directory.Build.props` | Shared `net8.0`, nullable, implicit usings |
| `dotnet-tools.json` | Local `dotnet-ef` tool |

- Environment variables used (names and purpose only, never values): `DOTNET_ROLL_FORWARD` (README: run net8.0 projects on a newer runtime); standard ASP.NET Core overrides such as `ConnectionStrings__AlertDb`.
- Secrets come from: `TO_BE_DISCOVERED` (the connection string is in `appsettings.json` with a LocalDB default; no vault or user-secrets setup found).

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): the six `AlertService.*` source projects and the two `*.Tests` projects.
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `appsettings*.json`, `launchSettings.json`, `*.csproj`, `Directory.Build.props`, `dotnet-tools.json`, `AlertService.sln`, `database/`, `README.md`, `.sdlc/README.md`, `AlertService.API.http`.
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `**/bin/`, `**/obj/`, `.vs/`, `Logs/`, `TestResults/`, `standards/`, pipeline files.
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/`, `AlertService.Data.SQL/Migrations/`, `database/02_AlertServiceDb_Migrations.sql`.

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
| `README.md` | Folder structure, dependency flow, API contract, health checks, run and EF migration commands |
| `.sdlc/README.md` | SDLC pipeline usage |
| `database/01_CreateDatabase.sql` | Database creation script |
| `database/02_AlertServiceDb_Migrations.sql` | Idempotent script generated from EF migrations |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
