# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API` | HTTP composition root | Controllers, business services, mappings, middleware, `Program.cs`, DI wiring |
| `AlertService.API.Tests` | Unit/integration tests for API | Controller + service tests (xUnit/Moq), health-check tests via `WebApplicationFactory` |
| `AlertService.Common` | Shared cross-cutting types | `Severity` enum, field-length/constant definitions used by every layer |
| `AlertService.Models` | Domain entities | `Alert` entity |
| `AlertService.DTO` | External contracts | `Requests/`, `Responses/` shapes returned/consumed by the API |
| `AlertService.Data` | Persistence abstraction | `Interfaces/IAlertRepository` — the only contract the service layer depends on |
| `AlertService.Data.SQL` | EF Core / SQL Server implementation | `AlertDbContext`, `Configurations/`, `Repositories/`, `Migrations/`, DI extension `AddSqlDataAccess` |
| `AlertService.Data.SQL.Tests` | Repository tests | EF Core InMemory/Sqlite-backed tests for `AlertService.Data.SQL` |
| `database/` | Hand-maintained DB scripts | `01_CreateDatabase.sql`, idempotent migration script `02_AlertServiceDb_Migrations.sql` |
| `standards/` | Engineering standards catalog | Source documents summarized in `standards-summary.md` |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs` (Serilog, DI registrations, middleware pipeline, health endpoints, optional startup migrations)
- Main UI/API/CLI/background entry surfaces: `AlertService.API/Controllers/AlertsController.cs` (`/api/alerts*`); `HealthCheckEndpointExtensions.MapAlertHealthEndpoints` (`/health/live`, `/health/ready`)
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs` implementing `IAlertService`
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/IAlertRepository` (contract) and `AlertService.Data.SQL/Repositories` (SQL Server implementation), `AlertService.Data.SQL/AlertDbContext`

## Notable Dependency Flow

```
AlertService.API ──► AlertService.DTO ──► AlertService.Common
        │            ──► AlertService.Models ──► AlertService.Common
        ├──► AlertService.Data (interfaces) ──► AlertService.Models ──► AlertService.Common
        └──► AlertService.Data.SQL ──► AlertService.Data
```

The service layer (`AlertManagementService`) depends only on `IAlertRepository`. The API
project references `AlertService.Data.SQL` solely to register it in DI
(`services.AddSqlDataAccess(configuration)`); no controller or service code calls into
`AlertService.Data.SQL` directly.

## Starter Notes

- Record only the important solution areas and control points.
- Keep this compact. It is meant to help `/analyze-story` narrow the impacted slice.
- Do not turn this into a full directory listing.