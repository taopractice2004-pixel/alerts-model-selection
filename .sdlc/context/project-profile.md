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
- Purpose: An ASP.NET Core (.NET 8) Web API microservice for managing alerts — create, read, update, deactivate, delete, plus filtering, paging, sorting, title search, and aggregate summary counts — backed by SQL Server via EF Core.
- Main responsibilities: Expose REST endpoints under `/api/alerts`; validate requests with data annotations; apply alert business rules in a service layer; persist through an `IAlertRepository`; expose `/health/live` and `/health/ready` probes.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# (nullable + implicit usings enabled) | C# 12 / `net8.0` |
| Framework | ASP.NET Core Web API | 8.0 |
| ORM / data access | Entity Framework Core (SQL Server provider) | 8.0.31 |
| Build / package tool | .NET SDK / MSBuild (NuGet) | .NET 8 SDK |
| Data store | SQL Server (LocalDB / Express / Docker) | 2022 |
| Logging | Serilog (Console + File sinks) | Serilog.AspNetCore 8.0.3 |
| API docs | Swashbuckle (Swagger) | 6.9.0 |
| Test framework | xUnit | 2.9.3 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | HTTP layer + business services + composition root (`Program.cs`, controllers, services, mappings, middleware). |
| `AlertService.API/Controllers/` | Thin ASP.NET controllers (routing, status codes). |
| `AlertService.API/Services/` | Business logic (`IAlertService`, `AlertManagementService`). |
| `AlertService.API/Mappings/` | Manual entity <-> DTO mapping extensions. |
| `AlertService.API/Middleware/` | `ExceptionHandlingMiddleware` (RFC7807 ProblemDetails). |
| `AlertService.API/Extensions/` | Health-check endpoint mapping. |
| `AlertService.Common/` | Shared enums (`Severity`) and constants (`AlertConstants`). |
| `AlertService.DTO/` | Request/response contracts (`Requests/`, `Responses/`). |
| `AlertService.Models/` | Domain entity (`Alert`). |
| `AlertService.Data/` | Data-access abstractions (`Interfaces/IAlertRepository`). |
| `AlertService.Data.SQL/` | EF Core implementation: `AlertDbContext`, configuration, repository, migrations, DI extensions. |
| `AlertService.API.Tests/` | Controller + service unit tests (xUnit, Moq). |
| `AlertService.Data.SQL.Tests/` | Repository tests (xUnit, EF Core InMemory). |
| `database/` | Hand-runnable SQL scripts (create DB + idempotent migration script). |
| `standards/` | Company coding standards (condensed into `standards-summary.md`). |

## 4. Architecture And Flow
- Style: Layered microservice (clean dependency flow), single Web API host.
- Layers and how they interact: Controller -> Service (`IAlertService`) -> Repository (`IAlertRepository`) -> `AlertDbContext` -> SQL Server. Mapping between `Alert` entity and DTOs happens in the service via `AlertMappingExtensions`.
- Main request / data flow: HTTP request -> model binding + data-annotation validation -> controller calls service -> service calls repository -> EF Core query/save -> entity mapped to response DTO -> JSON (enums serialized as strings).
- Entry points and composition root: `AlertService.API/Program.cs` (DI registration, Serilog, Swagger, health checks, exception middleware, optional migrate-on-startup).
- Architecture rules observed: Controllers never touch the DbContext or repository directly; the service layer depends only on `IAlertRepository` (abstraction in `AlertService.Data`), not on `Data.SQL`; the API project references `Data.SQL` solely to register it in DI; DTOs are separate from the `Alert` persistence entity.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API host | `AlertService.API` | HTTP endpoints, DI composition root, business services, mapping, middleware. |
| Common | `AlertService.Common` | Shared `Severity` enum and `AlertConstants` (paging, sorting, length limits). |
| DTO | `AlertService.DTO` | Request/response contracts with validation attributes. |
| Models | `AlertService.Models` | `Alert` domain entity. |
| Data (abstractions) | `AlertService.Data` | `IAlertRepository` interface. |
| Data.SQL | `AlertService.Data.SQL` | EF Core DbContext, entity configuration, repository, migrations, DI + health-check extensions. |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Thin controller; `[ApiController]`, attribute routing, `CancellationToken`, `ProducesResponseType`. |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Constructor DI, `ArgumentNullException.ThrowIfNull`, logging, maps entity<->DTO. |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core, `AsNoTracking` reads, paging/sorting, returns tuples. |
| Entity configuration | `AlertService.Data.SQL/Configurations/AlertConfiguration.cs` | `IEntityTypeConfiguration<Alert>`; enum stored as string. |
| DTO + validation | `AlertService.DTO/Requests/AlertQueryRequest.cs` | Data-annotation attributes + `IValidatableObject`. |
| External integration client | `NOT_APPLICABLE` | No external service clients in this repository. |
| UI component | `NOT_APPLICABLE` | No frontend in this repository. |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | xUnit `[Fact]`, Moq, `NullLogger`, fixed `TimeProvider`. |

## 7. Testing Context
- Test framework: xUnit (`[Fact]`, `Assert.*`).
- Mocking and assertion libraries: Moq for mocking; xUnit `Assert` for assertions; `NullLogger<T>` for logging; `Mock<TimeProvider>` for deterministic time.
- Test location and naming style: Separate test projects mirroring the production project (`AlertService.API.Tests`, `AlertService.Data.SQL.Tests`); classes `<ClassUnderTest>Tests`; methods `Method_Scenario_ExpectedResult`.
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`; repository tests use EF Core InMemory (`AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`).
- Coverage tooling: `coverlet.collector` referenced in test projects (`--collect:"XPlat Code Coverage"`).
- Coverage target for new or changed code: `NOT_CONFIGURED` (no explicit threshold defined in the repository).
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Base config: `ConnectionStrings:AlertDb`, `Database:ApplyMigrationsOnStartup`, Serilog sinks. |
| `AlertService.API/appsettings.Development.json` | Dev overrides: migrate-on-startup enabled, verbose Serilog levels. |
| `Directory.Build.props` | Shared build settings (`net8.0`, nullable, implicit usings). |
| `dotnet-tools.json` | Local tool manifest (`dotnet-ef` 8.0.31). |
| `AlertService.API/Properties/launchSettings.json` | Local launch profiles / ports. |

- Environment variables used (names and purpose only, never values): `DOTNET_ROLL_FORWARD` (run on a newer runtime than net8.0); standard ASP.NET Core `ASPNETCORE_ENVIRONMENT`.
- Secrets come from: configuration files / connection strings only (no secret store configured). The `AlertDb` connection string lives in `appsettings*.json`.

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.*/` projects and their `*.Tests` counterparts.
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `*.csproj`, `Directory.Build.props`, `dotnet-tools.json`, `appsettings*.json`, `database/`, `README.md`.
- Skip by default (dependencies, generated output, binaries, caches): `**/bin/`, `**/obj/`, `AlertService.API/Logs/`, `standards/`, pipeline files (`.github/skills/`, `.sdlc/templates/`, `.sdlc/work/`).
- Do not modify without approval (generated, protected): `AlertService.Data.SQL/Migrations/`, `database/`, `standards/`.

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
| `README.md` | Full overview: architecture, endpoints, health checks, run/test and EF migration commands. |
| `standards/` | Company coding, API, database, and service-architecture standards. |
| `database/01_CreateDatabase.sql` | Creates the `AlertServiceDb` database. |
| `database/02_AlertServiceDb_Migrations.sql` | Idempotent schema script generated from EF migrations. |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
