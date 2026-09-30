# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services (composition root) | Controllers, service layer, mappings, middleware, `Program.cs`, appsettings — primary place for API/business changes |
| `AlertService.API/Controllers/AlertsController.cs` | REST controller | HTTP routing/status codes for `/api/alerts` CRUD + summary + deactivate |
| `AlertService.API/Services/` | Business logic | `IAlertService` contract + `AlertManagementService` implementation |
| `AlertService.API/Mappings/` | Mapping | Entity <-> DTO conversion |
| `AlertService.API/Middleware/` | Cross-cutting | `ExceptionHandlingMiddleware` -> ProblemDetails |
| `AlertService.API/Extensions/` | Composition helpers | Health check + endpoint registration |
| `AlertService.DTO/` | API contracts | Request/response models (`Requests/`, `Responses/`) |
| `AlertService.Common/` | Shared kernel | `Severity` enum, `AlertConstants` (field lengths) |
| `AlertService.Models/` | Domain entities | `Alert` entity |
| `AlertService.Data/` | Data-access abstractions | `IAlertRepository` (no DB dependency) |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext`, EF configurations, repositories, migrations, DI extension |
| `AlertService.API.Tests/` | Unit/integration tests | Controller + service tests, health-check tests (xUnit/Moq) |
| `AlertService.Data.SQL.Tests/` | Repository tests | EF Core InMemory/Sqlite-backed tests |
| `database/` | SQL scripts | Create-database + idempotent migration script |
| `standards/` | Engineering standards | Coding/backend/API/service/database/frontend/UI rules |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`
- Main UI/API/CLI/background entry surfaces: `AlertsController` (`/api/alerts`), health endpoints (`/health`)
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
- Main persistence or integration boundaries: `IAlertRepository` (abstraction) implemented by
  `AlertService.Data.SQL` over `AlertDbContext`

## Notable Dependency Flow

- `API -> DTO -> Common`
- `API -> Data (interfaces) -> Models -> Common`
- `API -> Data.SQL -> Data` (Data.SQL wired into DI only, via `services.AddSqlDataAccess(configuration)`)
- Service layer depends only on `IAlertRepository`, not on `Data.SQL`.

## Starter Notes

- Compact map for `/analyze-story` scoping; not a full directory listing.
- Update via `/refresh-repo-context` when the solution structure changes materially.