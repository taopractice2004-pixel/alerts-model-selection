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
- Purpose: `AlertService` is a .NET 8 ASP.NET Core Web API for creating, querying, summarizing, updating, deactivating, and deleting alerts backed by SQL Server.
- Main responsibilities: `Expose REST endpoints, enforce alert business rules in a service layer, persist alerts through EF Core repositories, and publish liveness/readiness health checks.`

## 2. Technology Stack
| Area | Technology | Version |
|---|---|---|
| Language | `C#` | `net8.0` |
| Framework | `ASP.NET Core Web API, EF Core, Serilog, Swashbuckle` | `.NET 8, EF Core 8.0.31` |
| Build / package tool | `dotnet CLI, NuGet, local dotnet-ef tool` | `.NET SDK 8, dotnet-ef 8.0.31` |
| Data store | `SQL Server via EF Core SQL Server provider` | `EF Core provider 8.0.31` |
| Test framework | `xUnit, Moq, ASP.NET Core TestHost/WebApplicationFactory` | `xUnit 2.9.3, Moq 4.20.72` |

## 3. Repository Structure
Important folders only (about 15 rows at most).

| Path | Purpose |
|---|---|
| `AlertService.API/` | HTTP entry point, DI composition root, controllers, mappings, middleware, and business services |
| `AlertService.Common/` | Shared enums and constants used across layers |
| `AlertService.DTO/` | Request and response contracts for the API |
| `AlertService.Models/` | Core domain entity models |
| `AlertService.Data/` | Persistence abstractions such as repository interfaces |
| `AlertService.Data.SQL/` | SQL Server implementation of data access, EF Core context, configuration, and migrations |
| `AlertService.API.Tests/` | API-facing tests for controllers, services, and health endpoints |
| `AlertService.Data.SQL.Tests/` | Repository tests using EF Core test providers |
| `database/` | Database creation and generated migration deployment scripts |
| `standards/` | Company standards used only as setup inputs for the condensed summary |
| `.sdlc/context/` | Cached repository profile, standards summary, and command/context manifest |
| `.github/instructions/` | Path-based instruction entry points that route work to the cached standards summary |

## 4. Architecture And Flow
- Style: `Layered .NET microservice-style API with separate projects for API, DTOs, domain models, data abstractions, and SQL persistence.`
- Layers and how they interact: `Controller -> IAlertService/AlertManagementService -> IAlertRepository -> AlertRepository/AlertDbContext -> SQL Server, with mapping extensions translating between DTOs and models.`
- Main request / data flow: `Program.cs configures services and middleware, controllers accept requests and delegate to the service layer, the service layer applies business rules and logging, and repositories execute EF Core queries/commands against AlertDbContext.`
- Entry points and composition root: `AlertService.API/Program.cs` is the application entry point and DI composition root; `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` registers DbContext, repository, health checks, and migration helpers.
- Architecture rules observed: `Controllers stay thin, services depend on repository interfaces instead of EF Core directly, repositories are the only layer that talks to DbContext, DTO contracts are separate from domain models, and unhandled errors are normalized through middleware as ProblemDetails.`

## 5. Modules
| Name | Path | Responsibility |
|---|---|---|
| `AlertService.API` | `AlertService.API/` | Hosts the HTTP API, middleware, DI setup, logging, and business services |
| `AlertService.Common` | `AlertService.Common/` | Holds shared constants and enums such as severity and paging/sorting defaults |
| `AlertService.DTO` | `AlertService.DTO/` | Defines request and response DTOs used by controllers and services |
| `AlertService.Models` | `AlertService.Models/` | Defines the `Alert` entity model |
| `AlertService.Data` | `AlertService.Data/` | Defines repository contracts used by the service layer |
| `AlertService.Data.SQL` | `AlertService.Data.SQL/` | Implements EF Core SQL Server persistence, migrations, and health-check-backed DbContext registration |
| `AlertService.API.Tests` | `AlertService.API.Tests/` | Verifies controller, service, and API health endpoint behavior |
| `AlertService.Data.SQL.Tests` | `AlertService.Data.SQL.Tests/` | Verifies repository query/filter/sort behavior under EF Core test providers |

## 6. Pattern Examples
One representative existing file per common kind of change. Later stages copy these instead of searching.

| Change type | Example file | Notes |
|---|---|---|
| API endpoint / controller | `AlertService.API/Controllers/AlertsController.cs` | Attribute-routed controller with typed responses and cancellation-token flow |
| Service / business logic | `AlertService.API/Services/AlertManagementService.cs` | Service layer with logging, null checks, mapping, and repository delegation |
| Repository / data access | `AlertService.Data.SQL/Repositories/AlertRepository.cs` | EF Core repository with filtering, sorting, paging, and `AsNoTracking` reads |
| External integration client | `NOT_APPLICABLE` | No dedicated external service client exists in this repository |
| UI component | `NOT_APPLICABLE` | No frontend/UI project exists in this repository |
| Unit test | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` | xUnit + Moq test style for controller behaviors and DTO validation |

## 7. Testing Context
- Test framework: `xUnit across dedicated test projects.`
- Mocking and assertion libraries: `Moq for controller/service dependency mocking; xUnit assertions; ASP.NET Core WebApplicationFactory for HTTP-level tests; EF Core InMemory and SQLite providers for repository tests.`
- Test location and naming style: `Tests live in AlertService.API.Tests/ and AlertService.Data.SQL.Tests/; files follow ClassNameTests.cs and methods use Behavior_ExpectedResult naming.`
- Representative test file: `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- Coverage tooling: `coverlet.collector via dotnet test --collect:"XPlat Code Coverage"`
- Coverage target for new or changed code: `90% when that project rule applies.`
- Test, build, and coverage commands: see `commands` in `manifest.json`

## 8. Configuration
| File | Purpose |
|---|---|
| `AlertService.API/appsettings.json` | Base application settings including connection-string key name, migration toggle, and Serilog sinks/levels |
| `AlertService.API/appsettings.Development.json` | Development overrides, including automatic migration application and more verbose logging |
| `AlertService.API/Properties/launchSettings.json` | Local launch profile, development URLs, and local environment selection |
| `Directory.Build.props` | Shared .NET build settings for target framework, nullable, and implicit usings |
| `dotnet-tools.json` | Local tool manifest for `dotnet-ef` |
| `database/01_CreateDatabase.sql` | Database bootstrap script |
| `database/02_AlertServiceDb_Migrations.sql` | Generated idempotent EF migration deployment script |

- Environment variables used (names and purpose only, never values): `ASPNETCORE_ENVIRONMENT` selects the development profile in local launches; `DOTNET_ROLL_FORWARD` is documented in the README as an optional local runtime-compatibility override.
- Secrets come from: `Local development uses appsettings files for the SQL connection string; no user-secrets or vault integration is configured in source, and the production secret source is TO_BE_DISCOVERED.`

## 9. Context Policy
Summary only; the full machine-readable lists are in `contextPolicy` in `manifest.json`.
- Normal paths (source and tests): `AlertService.API/, AlertService.Common/, AlertService.DTO/, AlertService.Models/, AlertService.Data/, AlertService.Data.SQL/, AlertService.API.Tests/, AlertService.Data.SQL.Tests/`
- Read when relevant (config, infrastructure, build, CI/CD, docs, schemas, scripts): `README.md, AlertService.sln, Directory.Build.props, dotnet-tools.json, AlertService.API/appsettings*.json, AlertService.API/Properties/, database/`
- Skip by default (dependencies, generated output, binaries, caches, build and coverage artifacts): `**/bin/, **/obj/, AlertService.API/Logs/, **/TestResults/, standards/, .github/prompts/, .github/agents/, .github/skills/, .sdlc/templates/, .sdlc/work/`
- Do not modify without approval (generated, protected, vendor, legacy, team-owned): `standards/, AlertService.Data.SQL/Migrations/, database/02_AlertServiceDb_Migrations.sql, **/bin/, **/obj/`

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
| `README.md` | Repository overview, API surface, local run steps, and EF Core migration commands |
| `standards/coding-standards.md` | Company-wide coding, testing, and review expectations condensed into the repository summary |
| `standards/backend-dotnet-standards.md` | Backend-specific .NET naming, layout, async, and layering guidance |
| `standards/api-rest-standards.md` | REST contract, URI, status code, and backward-compatibility guidance |
| `standards/database-standards.md` | Query, transaction, and schema naming guidance for SQL work |
| `standards/service-architecture-standards.md` | Service-layer architecture and DI boundary expectations |

## 15. Requirements Summary (GREENFIELD)
Compact summary of the requirement document so stages do not reread it. `NOT_APPLICABLE` for EXISTING.
- Purpose and functional scope: `NOT_APPLICABLE`
- Actors and key business rules: `NOT_APPLICABLE`
- Integrations and external dependencies: `NOT_APPLICABLE`
- Non-functional and security requirements: `NOT_APPLICABLE`

## 16. Open Decisions (GREENFIELD)
Architecture or technical decisions no source settles. Resolved ones stay with their answer.
- `NOT_APPLICABLE`
