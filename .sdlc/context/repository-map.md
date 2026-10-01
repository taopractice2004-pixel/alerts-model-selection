# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP layer + business services (composition root) | `Controllers/`, `Services/`, `Mappings/`, `Middleware/`, `Extensions/`, `Program.cs`; owns DI wiring and HTTP pipeline |
| `AlertService.Common/` | Shared enums/constants | `Enums/` (e.g. `Severity`), `Constants/` (e.g. field lengths); referenced by nearly every project |
| `AlertService.DTO/` | Request/response contracts | `Requests/`, `Responses/`; validated via DataAnnotations |
| `AlertService.Models/` | Domain entities | `Alert.cs` |
| `AlertService.Data/` | Data-access abstractions | `Interfaces/IAlertRepository.cs` |
| `AlertService.Data.SQL/` | SQL Server EF Core implementation | `AlertDbContext.cs`, `Configurations/`, `Repositories/`, `Migrations/`, `Extensions/` (`AddSqlDataAccess`, `ApplyMigrationsAsync`) |
| `AlertService.API.Tests/` | Controller + service unit tests | xUnit/Moq; includes `HealthChecksTests.cs`, `TestInfrastructure/` |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit with EF Core InMemory/Sqlite |
| `database/` | Hand-maintained SQL assets | `01_CreateDatabase.sql`, `02_AlertServiceDb_Migrations.sql` (generated idempotent script) |
| `standards/` + `.github/instructions/standards/` | Engineering standards (full + compact) | See `.sdlc/context/standards-index.json` |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs` (DI registration, Serilog,
  middleware pipeline, health endpoints, controllers)
- Main UI/API/CLI/background entry surfaces: `AlertService.API/Controllers/AlertsController.cs`
  (`/api/alerts` REST endpoints), `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`
  (`/health/live`, `/health/ready`)
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
  (implements `IAlertService`)
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs`
  (contract) implemented by `AlertService.Data.SQL/Repositories/` against `AlertDbContext`

## Notable Dependency Flow

```
API ──► DTO ──► Common
 │ └──► Data (interfaces) ──► Models ──► Common
 └────► Data.SQL ──► Data
```
The service layer depends only on `IAlertRepository`; `AlertService.API` references
`AlertService.Data.SQL` solely to register it in DI (`AddSqlDataAccess`).

## Starter Notes

- Record only the important solution areas and control points.
- Keep this compact. It is meant to help `/analyze-story` narrow the impacted slice.
- Do not turn this into a full directory listing.