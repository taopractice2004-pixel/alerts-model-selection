# Repository Map

## Top-Level Areas

| Path | Purpose |
|---|---|
| `AlertService.API/` | API host, controllers, service-layer implementation, mappings, middleware, startup |
| `AlertService.Data/` | Persistence abstractions (`IAlertRepository`) |
| `AlertService.Data.SQL/` | EF Core SQL Server implementation (`AlertDbContext`, repositories, migrations, DI extensions) |
| `AlertService.Common/` | Shared enums/constants |
| `AlertService.Models/` | Domain entities |
| `AlertService.DTO/` | API request/response contracts |
| `AlertService.API.Tests/` | API/service/integration/health tests |
| `AlertService.Data.SQL.Tests/` | Data layer tests |
| `database/` | SQL bootstrap + idempotent migration scripts |
| `standards/` | Engineering standards used by SDLC prompts |
| `.github/prompts/` | SDLC stage command definitions |
| `.sdlc/` | SDLC framework templates, context cache, and workflow docs |

## Control Points

- Startup/composition root: `AlertService.API/Program.cs`
- Primary API controller: `AlertService.API/Controllers/AlertsController.cs`
- Primary business service: `AlertService.API/Services/AlertManagementService.cs`
- Repository contract: `AlertService.Data/Interfaces/IAlertRepository.cs`
- SQL registration boundary: `AlertService.Data.SQL/Extensions/DependencyInjection.cs`

## Build/Tooling Anchors

- Solution: `AlertService.sln`
- Shared build properties: `Directory.Build.props`
- Local .NET tools: `dotnet-tools.json`
