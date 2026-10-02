# AlertService – Alert Management Microservice

A small but enterprise-structured ASP.NET Core Web API (.NET 8) for managing alerts.
It uses SQL Server + EF Core, the repository pattern, a service layer, Serilog, xUnit and Moq.

## Folder structure

```
AlertService.sln
Directory.Build.props              # shared settings: net8.0, nullable, implicit usings
dotnet-tools.json                  # local tool manifest (dotnet-ef 8.0.31)
database/
  01_CreateDatabase.sql            # creates the AlertServiceDb database
  02_AlertServiceDb_Migrations.sql # idempotent script generated from the EF migrations
AlertService.API/                  # HTTP layer + business services (composition root)
  Controllers/AlertsController.cs      # HTTP only: routing, status codes
  Services/IAlertService.cs            # business logic contract
  Services/AlertManagementService.cs   # business logic implementation
  Mappings/AlertMappingExtensions.cs   # entity <-> DTO mapping
  Middleware/ExceptionHandlingMiddleware.cs  # unhandled errors -> 500 ProblemDetails
  Program.cs, appsettings*.json, AlertService.API.http
AlertService.Common/               # shared enums/constants (Severity, field lengths)
AlertService.DTO/                  # request/response contracts (Requests/, Responses/)
AlertService.Models/               # domain entities (Alert)
AlertService.Data/                 # data-access abstractions (IAlertRepository)
AlertService.Data.SQL/             # SQL Server implementation (DbContext, config, repository, migrations)
AlertService.API.Tests/            # controller + service unit tests (xUnit, Moq)
AlertService.Data.SQL.Tests/       # repository tests (xUnit, EF Core InMemory)
```

### Dependency flow

```
API ──► DTO ──► Common
 │ └──► Data (interfaces) ──► Models ──► Common
 └────► Data.SQL ──► Data
```

The service layer depends only on `IAlertRepository`. The API project knows about `Data.SQL`
only to register it in DI (`services.AddSqlDataAccess(configuration)`).

## API

| Method | Route               | Success          | Errors      |
|--------|---------------------|------------------|-------------|
| POST   | `/api/alerts`       | 201 + `Location`, or 200 + existing alert with `X-Duplicate-Suppressed: true` | 400         |
| GET    | `/api/alerts`       | 200 + paged body | 400         |
| GET    | `/api/alerts/summary` | 200 + aggregate body | -      |
| GET    | `/api/alerts/trends` | 200 + daily buckets | 400    |
| GET    | `/api/alerts/{id}`  | 200              | 404         |
| PUT    | `/api/alerts/{id}`  | 200              | 400, 404    |
| PATCH  | `/api/alerts/{id}/deactivate` | 200      | 404         |
| DELETE | `/api/alerts/{id}`  | 204              | 404         |
| POST   | `/api/alerts/{id}/tags` | 200 + alert  | 400, 404    |
| DELETE | `/api/alerts/{id}/tags/{tag}` | 204    | 404         |

`severity` is sent and returned as a string: `Low`, `Medium`, `High` or `Critical`.

`POST /api/alerts` suppresses near-duplicates: if an active alert with the same title (case-insensitive) and severity
was created within the last `AlertSuppression:DuplicateWindowMinutes` minutes (`appsettings.json`, `15` by default;
`0` or a missing value disables suppression), no row is created and the existing alert is returned with `200 OK` and the
header `X-Duplicate-Suppressed: true`.

`GET /api/alerts` supports optional query parameters:
- `isActive=true|false`
- `severity=Low|Medium|High|Critical` (or numeric enum values `1..4`)
- `page=1` (minimum `1`)
- `pageSize=20` (range `1..100`)
- `sortBy=createdDate|severity|title` (default `createdDate`)
- `sortDirection=asc|desc` (default `desc`)
- `search=<title fragment>` (case-insensitive, max `200` chars)
- `tag=<name>` (exact, case-insensitive, max `30` chars)

This endpoint now returns a paged response wrapper. That is a breaking response-contract change from the previous raw array response.

`GET /api/alerts/trends` supports one optional query parameter: `days=7` (range `1..90`). It returns one bucket per UTC
calendar day for the last `days` days (ending today, oldest first); days and severities without alerts have count `0`.
An out-of-range or non-numeric `days` returns `400` with `ValidationProblemDetails`.

Examples:
- `GET /api/alerts?severity=Critical`
- `GET /api/alerts?severity=Critical&isActive=true&page=2&pageSize=10&sortBy=title&sortDirection=asc&search=disk`
- `GET /api/alerts/summary`
- `GET /api/alerts/trends?days=30`
- `PATCH /api/alerts/1/deactivate`

```json
GET /api/alerts?page=1&pageSize=2&sortBy=createdDate&sortDirection=desc&search=disk
{
  "items": [
    {
      "id": 12,
      "title": "Disk usage high",
      "description": "85% used",
      "severity": "High",
      "createdDate": "2026-09-01T00:00:00Z",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 1,
  "totalPages": 1
}
```

```json
GET /api/alerts/summary
{
  "totalCount": 5,
  "activeCount": 3,
  "inactiveCount": 2,
  "severityCounts": {
    "low": 1,
    "medium": 1,
    "high": 1,
    "critical": 2
  }
}
```

```json
GET /api/alerts/trends?days=2
{
  "days": 2,
  "buckets": [
    { "date": "2026-10-01", "totalCount": 0, "severityCounts": { "low": 0, "medium": 0, "high": 0, "critical": 0 } },
    { "date": "2026-10-02", "totalCount": 3, "severityCounts": { "low": 1, "medium": 0, "high": 1, "critical": 1 } }
  ]
}
```

```json
POST /api/alerts
{ "title": "CPU usage high", "description": "CPU above 90%", "severity": "High", "isActive": true }
```

## Health checks

The API exposes two probe endpoints for orchestrators and load balancers:

| Method | Route | Purpose | Success | Failure |
|--------|-------|---------|---------|---------|
| GET | `/health/live` | Process liveness only; does not touch SQL Server | `200 Healthy` | `503` if the process health check ever reports unhealthy |
| GET | `/health/ready` | Readiness backed by `AlertDbContext` database connectivity | `200 Healthy` | `503 Unhealthy` when the database cannot be reached |

Both endpoints return a minimal JSON document containing only the overall `status`, total
`duration`, and a `checks` array with each check's `name`, `status`, and `duration`.

Example Kubernetes probes:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

## Prerequisites

- .NET 8 SDK (or a newer SDK; see the note below)
- SQL Server: LocalDB (ships with Visual Studio), SQL Server Express/Developer, or Docker

> **Only have a newer runtime (e.g. .NET 10)?** The projects target `net8.0`. To run them
> without installing the .NET 8 runtime, set `DOTNET_ROLL_FORWARD=Major` first
> (PowerShell: `$env:DOTNET_ROLL_FORWARD='Major'`).

## Run locally

```powershell
# 1. Restore tools (dotnet-ef) and packages
dotnet tool restore
dotnet restore

# 2. Configure the connection string (AlertService.API/appsettings.json -> ConnectionStrings:AlertDb)
#    Default: Server=(localdb)\MSSQLLocalDB;Database=AlertServiceDb;...
#    Docker alternative:
#      docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_strong_Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
#      "AlertDb": "Server=localhost,1433;Database=AlertServiceDb;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True"

# 3. Create the database schema (pick one)
dotnet ef database update --project AlertService.Data.SQL --startup-project AlertService.API
#    ...or just run the app in Development: it applies migrations on startup
#    (Database:ApplyMigrationsOnStartup = true in appsettings.Development.json)
#    ...or run database/01_CreateDatabase.sql then database/02_AlertServiceDb_Migrations.sql

# 4. Run the API
dotnet run --project AlertService.API --launch-profile https
#    Swagger UI: https://localhost:7080/swagger
#    Or use AlertService.API/AlertService.API.http from VS / VS Code / Rider

# 5. Run tests
dotnet test
```

Logs are written to the console and to `AlertService.API/Logs/alertservice-<date>.log`.

## EF Core migration commands

Run these from the solution root. `--project` is where migrations live and `--startup-project`
supplies the configuration and connection string.

```powershell
# Add a migration after changing an entity/configuration
dotnet ef migrations add <Name> --project AlertService.Data.SQL --startup-project AlertService.API --output-dir Migrations

# Apply migrations to the database
dotnet ef database update --project AlertService.Data.SQL --startup-project AlertService.API

# Remove the last (unapplied) migration
dotnet ef migrations remove --project AlertService.Data.SQL --startup-project AlertService.API

# Generate an idempotent SQL script (for DBAs / CI deployments)
dotnet ef migrations script --idempotent --project AlertService.Data.SQL --startup-project AlertService.API --output database/02_AlertServiceDb_Migrations.sql

# Check whether the model has changes without a migration (useful in CI)
dotnet ef migrations has-pending-model-changes --project AlertService.Data.SQL --startup-project AlertService.API
```

## Design notes

- **Controllers** only translate HTTP to service calls and results to status codes. Validation
  comes from DataAnnotations on the DTOs, and `[ApiController]` returns 400 automatically.
- **Service** (`AlertManagementService`) owns business rules: it sets `CreatedDate` (UTC, taken
  from an injected `TimeProvider` so tests can control it), trims input, keeps `CreatedDate`
  unchanged on update, and logs. It returns `null`/`false` for "not found" and the controller turns that into a 404.
- **Repository** (`AlertRepository`) is the only code that talks to EF Core.
- `Severity` is stored as a string column. `CreatedDate` is read back as a UTC `DateTime`.
