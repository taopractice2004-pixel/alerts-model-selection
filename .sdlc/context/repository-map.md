# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `AlertService.API/` | Web API and composition root | Controllers, service layer, middleware, mappings, health endpoints, configuration |
| `AlertService.DTO/` | External request/response contracts | Query, create, update, paged, and summary DTOs |
| `AlertService.Common/` | Shared constants and enums | Includes `Severity` and alert query defaults |
| `AlertService.Models/` | Domain model | `Alert` entity |
| `AlertService.Data/` | Persistence abstractions | `IAlertRepository`; no provider dependency |
| `AlertService.Data.SQL/` | SQL Server persistence | EF `AlertDbContext`, configuration, repository, migrations, DI extensions |
| `AlertService.API.Tests/` | API/service tests | xUnit, Moq, and health-check host tests |
| `AlertService.Data.SQL.Tests/` | Repository tests | xUnit with EF Core InMemory and SQLite |
| `database/` | Database scripts | Creation and idempotent migration scripts |
| `standards/` | Engineering standards | Coding, backend, API, service, database, frontend, and UI guidance |

## Runtime Control Points

- Main startup/composition points: `AlertService.API/Program.cs`
- Main API entry surfaces: `AlertService.API/Controllers/AlertsController.cs`; health mapping extensions expose `/health/live` and `/health/ready`
- Main business orchestration points: `AlertService.API/Services/AlertManagementService.cs`
- Main persistence or integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs` and `AlertService.Data.SQL/Repositories/AlertRepository.cs`

## Notable Dependency Flow

- `API -> DTO/Common/Models/Data/Data.SQL`; `Data.SQL -> Data -> Models/Common`; test projects reference the production slice they exercise.
- Controllers translate HTTP concerns; the service owns business rules; the repository owns EF Core queries and persistence.