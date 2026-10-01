# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP delivery layer | Controllers, middleware, composition root, and service orchestration entry point |
| `AlertService.API/Services/` | Business/service layer | Primary business behavior for alert workflows |
| `AlertService.Data/` | Data abstractions | Repository contracts consumed by service layer |
| `AlertService.Data.SQL/` | Persistence implementation | EF Core DbContext, entity configuration, repository implementations, migrations |
| `AlertService.Models/` | Domain model | Core `Alert` entity and domain fields |
| `AlertService.DTO/` | API contracts | Request/response DTOs including paging and summary response shapes |
| `AlertService.Common/` | Shared primitives | Cross-project enums and constants (for example `Severity`) |
| `AlertService.API.Tests/` | API/service tests | Unit tests for controllers, service logic, and health-check behavior |
| `AlertService.Data.SQL.Tests/` | Data-layer tests | Repository tests against EF InMemory setup |
| `database/` | SQL operational scripts | DB creation and idempotent migration SQL artifacts |
| `standards/` | Full standards source | Human-readable standards referenced by compact instruction files |
| `.github/instructions/standards/` | Compact enforceable rules | Auto-applied rules routed by file globs and used by coding agents |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`, `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs`
- Main UI/API/CLI/background entry surfaces: REST API in `AlertService.API/Controllers/AlertsController.cs`; health endpoints mapped via `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
- Main persistence or integration boundaries: `AlertService.Data.Interfaces/IAlertRepository.cs`, `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`

## Notable Dependency Flow

- `AlertService.API` -> `AlertService.API/Services` -> `AlertService.Data` (interfaces) -> `AlertService.Data.SQL` (implementation) -> SQL Server
- DTO and domain boundaries are separated (`AlertService.DTO` vs `AlertService.Models`) with mapping extensions in API layer