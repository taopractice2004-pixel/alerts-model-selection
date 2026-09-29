# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | HTTP API and composition root | Entry point for controllers, middleware, DI, logging, health checks, and service wiring |
| `AlertService.API/Controllers/` | REST endpoints | First stop for route, status-code, and request/response behavior changes |
| `AlertService.API/Services/` | Business/service layer | Main orchestration and business-rule slice for alert operations |
| `AlertService.API/Mappings/` | DTO/domain mapping | Contract-to-model translation surface |
| `AlertService.API/Middleware/` | Cross-cutting HTTP behavior | Global exception handling and related pipeline behavior |
| `AlertService.Data/` | Data-access abstractions | Repository interfaces that decouple API/services from persistence implementation |
| `AlertService.Data.SQL/` | SQL Server persistence implementation | `DbContext`, EF configuration, repositories, extensions, migrations |
| `AlertService.DTO/` | API contracts | Request and response shapes consumed by controllers and clients |
| `AlertService.Models/` | Domain entities | Core alert entity definitions shared by service and data layers |
| `AlertService.Common/` | Shared constants/enums | Shared primitives such as severity values and constants |
| `AlertService.API.Tests/` | API/service tests | xUnit coverage for controllers, services, and health-check behavior |
| `AlertService.Data.SQL.Tests/` | Persistence tests | Repository-level verification for EF/data-access behavior |
| `database/` | Operational SQL artifacts | Manual DB creation and idempotent migration scripts |
| `standards/` | Repository engineering standards | Scope-specific coding guidance consumed by SDLC stages |
| `.github/` | SDLC prompts, agents, and repo instructions | Defines the manual Copilot workflow used in this repository |
| `.sdlc/` | Cached analysis and work artifacts | Repository context cache plus per-work-item state |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`, `AlertService.Data.SQL/Extensions/*`, `AlertService.API/Extensions/*`
- Main UI/API/CLI/background entry surfaces: `AlertService.API/Controllers/AlertsController.cs`, health endpoints mapped from `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/*`, `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Repositories/*`

## Notable Dependency Flow

- `AlertService.API` -> `AlertService.DTO`, `AlertService.Models`, `AlertService.Common`, `AlertService.Data`, `AlertService.Data.SQL` for composition
- `AlertService.API/Controllers` -> `AlertService.API/Services` -> `AlertService.Data` interfaces -> `AlertService.Data.SQL` repositories -> SQL Server
- Tests target API/service behavior through `WebApplicationFactory`, mocks, and EF-backed repository test doubles

## Starter Notes

- Prefer the narrowest owning layer: contracts in DTO, orchestration in services, persistence in `Data.SQL`.
- Use the test projects that mirror the touched layer before widening validation scope.