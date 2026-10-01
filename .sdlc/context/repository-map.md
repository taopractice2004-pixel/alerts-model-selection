# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API\` | API delivery layer and composition root | Controllers, middleware, service implementation, app startup, health endpoints, and runtime config live here |
| `AlertService.Common\` | Shared constants and enums | Small shared dependency used by models and DTOs |
| `AlertService.DTO\` | External request/response contracts | Query validation and API payload shapes are defined here |
| `AlertService.Models\` | Core domain entities | Persistent model types such as `Alert` live here |
| `AlertService.Data\` | Data-access abstractions | Repository interfaces isolate the service layer from EF Core |
| `AlertService.Data.SQL\` | SQL Server persistence implementation | `AlertDbContext`, entity configuration, repository, health-check registration, and EF migrations |
| `AlertService.API.Tests\` | API/service and health-check tests | Primary validation surface for controllers, service behavior, and host wiring |
| `AlertService.Data.SQL.Tests\` | Repository tests | Persistence-focused tests using EF Core test providers |
| `database\` | Database bootstrap artifacts | Manual database creation script plus generated idempotent migration SQL |
| `standards\` | Full engineering standards source | Referenced by setup/analysis stages; compact instruction files route from here |

## Runtime Control Points

- Main startup/composition points: `AlertService.API\Program.cs`, `AlertService.Data.SQL\Extensions\ServiceCollectionExtensions.cs`
- Main UI/API/CLI/background entry surfaces: `AlertService.API\Controllers\AlertsController.cs`, `AlertService.API\Extensions\HealthCheckEndpointExtensions.cs`
- Main business orchestration points: `AlertService.API\Services\AlertManagementService.cs`
- Main persistence or integration boundaries: `AlertService.Data\Interfaces\IAlertRepository.cs`, `AlertService.Data.SQL\Repositories\AlertRepository.cs`, `AlertService.Data.SQL\AlertDbContext.cs`

## Notable Dependency Flow

- `AlertService.API` -> `AlertService.DTO` / `AlertService.Common` for contracts and shared constants
- `AlertService.API` -> `AlertService.Data` for repository abstraction, with SQL implementation registered from `AlertService.Data.SQL`
- `AlertService.Data` -> `AlertService.Models` and `AlertService.Common`
- Test projects depend on the concrete API or SQL projects rather than on solution-wide helpers