# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services (composition root) | Controllers, Services, Mappings, Middleware, `Program.cs`, health-check extensions, appsettings |
| `AlertService.Common/` | Shared enums/constants | `Severity` enum, `AlertConstants` (field lengths) |
| `AlertService.DTO/` | Request/response contracts | `Requests/` (Create, Update, Query), `Responses/` (Alert, Summary, SeverityCounts, Paged) |
| `AlertService.Models/` | Domain entities | `Alert` entity |
| `AlertService.Data/` | Data-access abstractions | `IAlertRepository` interface only |
| `AlertService.Data.SQL/` | SQL Server implementation | `AlertDbContext`, `AlertConfiguration`, `AlertRepository`, `Migrations/`, DI extension |
| `AlertService.API.Tests/` | API/service unit + health-check tests | xUnit, Moq, WebApplicationFactory |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit, EF Core InMemory |
| `database/` | SQL assets | `01_CreateDatabase.sql`, idempotent migrations script |
| `standards/` | Engineering standards (full source) | Summarized in `standards-summary.md` |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`;
  `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` (`AddSqlDataAccess`)
- Main UI/API/CLI/background entry surfaces: `AlertsController` (`/api/alerts*`);
  `HealthCheckEndpointExtensions` (`/health/live`, `/health/ready`); Swagger UI
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
  (`IAlertService`)
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs`
  → `AlertService.Data.SQL/Repositories/AlertRepository.cs` over `AlertDbContext`

## Notable Dependency Flow

- `API ──► DTO ──► Common`
- `API ──► Data (interfaces) ──► Models ──► Common`
- `API ──► Data.SQL ──► Data` (Data.SQL referenced by API only for DI registration)
- Service layer depends only on `IAlertRepository`, not on EF Core.

## Starter Notes

- Compact map to help `/analyze-story` narrow the impacted slice. Not a full directory listing.