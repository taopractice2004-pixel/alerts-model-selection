# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | ASP.NET Core delivery layer and composition root | Main runtime project; holds `Program.cs`, controllers, middleware, mappings, DI wiring, and service implementation |
| `AlertService.Common/` | Shared enums and constants | Cross-project shared primitives such as severity and field constraints |
| `AlertService.DTO/` | External request/response contracts | API input/output models; likely impact area for contract changes |
| `AlertService.Models/` | Domain entity models | Shared entity definitions used by service and persistence layers |
| `AlertService.Data/` | Persistence abstractions | Defines `IAlertRepository`; service logic depends here instead of EF Core directly |
| `AlertService.Data.SQL/` | SQL Server persistence implementation | EF Core `DbContext`, repository, configuration, migrations, and DI extensions |
| `AlertService.API.Tests/` | API and service tests | Covers controllers, health checks, and service behavior |
| `AlertService.Data.SQL.Tests/` | Persistence tests | Covers repository/data behavior with test database providers |
| `database/` | Operational SQL assets | Database creation and idempotent migration scripts |
| `standards/` | Engineering standards catalog | Source for story-specific standards selection during later stages |
| `.github/` and `.sdlc/` | SDLC automation assets | Prompts, agent instructions, and compact cached context used by this workflow |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`
- Main UI/API/CLI/background entry surfaces: `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`
- Main business orchestration points: `AlertService.API/Services/IAlertService.cs`, `AlertService.API/Services/AlertManagementService.cs`
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs`, `AlertService.Data.SQL/AlertDbContext.cs`

## Notable Dependency Flow

- `AlertService.API.Controllers` -> `IAlertService` -> `IAlertRepository` -> `AlertService.Data.SQL` -> SQL Server
- `AlertService.API` uses `AlertService.DTO` for HTTP contracts and `AlertService.API/Mappings` to convert between DTOs and `AlertService.Models`
- `AlertService.Common` supplies shared enums/constants across API, DTO, model, and data projects
