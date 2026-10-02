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
- Purpose: An ASP.NET Core Web API microservice for managing alerts (create, query, update, deactivate, delete) with severity levels and paging/sorting/search.
- Main responsibilities: Expose a REST API for alert CRUD and aggregate summary; persist alerts to SQL Server via EF Core; enforce input validation and expose health-check probes for orchestrators.

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | C# | `net8.0` (nullable + implicit usings enabled) |
| Framework | ASP.NET Core Web API | 8.0 |
| Build / package tool | .NET SDK / MSBuild / NuGet | SDK 8 (roll-forward supported) |
| Data store | SQL Server via EF Core | EF Core 8.0.31 (dotnet-ef tool 8.0.31) |
| Logging | Serilog (Console + File sinks) | 8.x |
| Test framework | xUnit | 2.9.3 |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | HTTP layer, business services, composition root (`Program.cs`) |
| `AlertService.API/Controllers/` | Thin controllers (routing, status codes) |
| `AlertService.API/Services/` | Business logic (`IAlertService`, `AlertManagementService`) |
| `AlertService.API/Mappings/` | Entity <-> DTO mapping extensions |
| `AlertService.API/Middleware/` | Exception handling middleware |
| `AlertService.Common/` | Shared enums (`Severity`) and constants |
| `AlertService.DTO/` | Request/response contracts (`Requests/`, `Responses/`) |
| `AlertService.Models/` | Domain entities (`Alert`) |
| `AlertService.Data/` | Data-access abstractions (`IAlertRepository`) |
| `AlertService.Data.SQL/` | EF Core SQL Server impl: DbContext, configurations, repository, migrations |
| `AlertService.API.Tests/` | Controller + service unit tests, health-check integration tests |
| `AlertService.Data.SQL.Tests/` | Repository tests |
| `database/` | SQL scripts: create database + idempotent migration script |
| `standards/` | Company coding/architecture standards (condensed into standards-summary.md) |

## 4. Architecture And Flow
- Style: Layered microservice with dependency-inverted data access.
- Layers and how they interact: Controller -> Service (`IAlertService`) -> Repository (`IAlertRepository`) -> `AlertDbContext` -> SQL Server. DTOs separate the API contract from entities.
- Main request / data flow: HTTP request -> `AlertsController` -> `AlertManagementService` (maps DTO<->entity, logs) -> `AlertRepository` (EF Core queries) -> DB; response mapped back to DTOs.
- Entry points and composition root: `AlertService.API/Program.cs` (DI registration, Serilog, Swagger, health endpoints, middleware, optional startup migrations).
- Architecture rules observed: Controllers never touch EF Core or repositories directly (only `IAlertService`). The service layer depends only on `IAlertRepository`, never on EF Core. API references `Data.SQL` only to register DI via `AddSqlDataAccess`. DTOs are kept separate from domain entities.

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| API | `AlertService.API/` | HTTP endpoints, business services, composition root |
| Common | `AlertService.Common/` | Shared `Severity` enum and `AlertConstants` |
| DTO | `AlertService.DTO/` | Request/response contracts with DataAnnotations validation |
| Models | `AlertService.Models/` | `Alert` domain entity |
| Data | `AlertService.Data/` | Provider-agnostic `IAlertRepository` abstraction |
| Data.SQL | `AlertService.Data.SQL/` | EF Core SQL Server DbContext, configuration, repository, migrations |
| API.Tests | `AlertService.API.Tests/` | xUnit tests for controllers, services, health checks |
| Data.SQL.Tests | `AlertService.Data.SQL.Tests/` | xUnit tests for the repository |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Thin; `[ApiController]`, attribute routes, `ProducesResponseType`, `CancellationToken` |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Constructor DI, `ArgumentNullException.ThrowIfNull`, structured logging, DTO mapping |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core, `AsNoTracking`, filtered/paged queries, constants for defaults |
| DTO / contract + validation | `AlertService.DTO/Requests/CreateAlertRequest.cs` | DataAnnotations (`[Required]`, `[StringLength]`, `[EnumDataType]`) using `AlertConstants` |
| DI registration | `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` | `IServiceCollection` extension methods (DbContext, repositories, health checks) |
| External integration client | `NOT_APPLICABLE` | No external system clients |
| UI component | `NOT_APPLICABLE` | Backend-only service (no frontend) |
| Unit test | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` | xUnit `[Fact]`, Moq `Mock<IAlertRepository>`, `NullLogger`, fixed `TimeProvider` |

## 7. Testing Context
- Test framework: xUnit (2.9.3).
- Mocking and assertion libraries: Moq (4.20.72) for mocks; xUnit `Assert` for assertions; `Microsoft.AspNetCore.Mvc.Testing` for health-check integration tests via `WebApplicationFactory<Program>`; EF Core InMemory/Sqlite for repository tests.
- Test location and naming style: One test project per layer (`AlertService.API.Tests`, `AlertService.Data.SQL.Tests`); tests mirror source folders; classes named `<TypeUnderTest>Tests`; methods `Method_Scenario_ExpectedResult`.
- Representative test file: `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`.
- Coverage tooling: `coverlet.collector` (6.0.2) referenced in test projects (`dotnet test --collect:"XPlat Code Coverage"`).
- Coverage target for new or changed code: `NOT_CONFIGURED`.
- Test, build, and coverage commands: see `commands` in `manifest.json`.

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Connection string (`AlertDb`), Serilog config, `Database:ApplyMigrationsOnStartup` |
| `AlertService.API/appsettings.Development.json` | Development overrides (enables startup migrations) |
| `Directory.Build.props` | Shared build settings: `net8.0`, nullable, implicit usings |
| `dotnet-tools.json` | Local tool manifest (`dotnet-ef` 8.0.31) |
| `AlertService.API/Properties/launchSettings.json` | Launch profiles (http/https, Swagger) |

- Environment variables used (names and purpose only, never values): `DOTNET_ROLL_FORWARD` (run net8.0 projects on a newer runtime); standard `ASPNETCORE_ENVIRONMENT` for environment selection.
- Secrets come from: Connection string in `appsettings.json` for local/dev (LocalDB default). No dedicated vault or user-secrets configured.

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.*/` source and test projects (excluding `bin/`, `obj/`).
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `*.csproj`, `Directory.Build.props`, `appsettings*.json`, `dotnet-tools.json`, `database/`, `README.md`, `AlertService.sln`.
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `**/bin/`, `**/obj/`, `standards/`, pipeline files.
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `AlertService.Data.SQL/Migrations/`, `database/`, `standards/`.

## 10. Domain Glossary (developer-maintained)
Maps story language to code names. 10-20 terms.

| Term | Code name / location |
|---|---|
| Alert | `AlertService.Models/Alert.cs` |
| Severity (Low/Medium/High/Critical) | `AlertService.Common/Enums/Severity.cs` |
| Alert summary / counts | `AlertSummaryResponse`, `AlertRepository.GetSummaryAsync` |
| Paged result | `AlertService.DTO/Responses/PagedResponse.cs` |
| Deactivate | `AlertManagementService.DeactivateAsync` (PATCH endpoint) |

## 11. Known Pitfalls (developer-maintained)
- Readiness health check (`/health/ready`) requires SQL Server connectivity; `/health/live` does not.
- `GET /api/alerts` returns a paged wrapper (`PagedResponse<T>`), not a raw array (breaking contract change noted in README).

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
| `README.md` | Overview, folder structure, dependency flow, API table, run/migration commands |
| `standards/backend-dotnet-standards.md` | Backend .NET coding and architecture standards |
| `standards/database-standards.md` | Database modeling, query, and naming standards |
| `standards/api-rest-standards.md` | REST API conventions |
| `standards/service-architecture-standards.md` | Service architecture guidance |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
