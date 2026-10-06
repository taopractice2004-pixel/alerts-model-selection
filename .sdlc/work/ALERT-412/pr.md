# ALERT-412 Add UTC daily alert trends endpoint

## PR Description
Add a trends API for alerts that returns UTC day buckets for the last N days with total and per-severity counts, including zero-filled day and severity buckets. This change also enforces `days` query validation (default 7, range 1-90) with standard 400 validation responses.

## Story / Requirement Summary
- Work ID: ALERT-412
- Summary: Add GET /api/alerts/trends?days=N to return daily UTC alert-creation counts with severity breakdown for the last N days.
- Acceptance criteria:
  - AC1: Last N UTC calendar days (oldest first), one bucket per day, with total and per-severity counts, zero-filled days/severities.
  - AC2: Severity ordering: Low, Medium, High, Critical.
  - AC3: Invalid days (non-numeric or out of range) returns HTTP 400 ValidationProblemDetails.

## Implementation Summary
- Added `GET /api/alerts/trends` endpoint flow through controller, service contract/service implementation, and repository contract/SQL repository.
- Added trends response DTO shape for day + total + ordered severity counts.
- Implemented UTC day-window aggregation and zero-fill behavior for days and severities.
- Implemented query validation constraints for `days` via existing API model validation behavior.

## Changed-Files Summary (read-only diff)
Source: `git diff --name-only`

- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

Additional scoped file from work cache:
- AlertService.DTO/Responses/AlertTrendResponse.cs (new)

## AC -> Implementation / Validation Traceability
- AC1
  - Implementation:
    - AlertService.Data.SQL/Repositories/AlertRepository.cs (UTC day aggregation + oldest-first + zero-fill)
    - AlertService.API/Services/AlertManagementService.cs (maps repository output to response model)
    - AlertService.API/Controllers/AlertsController.cs (endpoint wiring/default days)
  - Validation:
    - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
    - AlertService.API.Tests/Services/AlertManagementServiceTests.cs
    - AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AC2
  - Implementation:
    - AlertService.Data.SQL/Repositories/AlertRepository.cs (severity-order-aligned output)
    - AlertService.API/Services/AlertManagementService.cs (mapped severity counts preserved)
  - Validation:
    - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
    - AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AC3
  - Implementation:
    - AlertService.API/Controllers/AlertsController.cs (days range constraint 1-90)
  - Validation:
    - AlertService.API.Tests/Controllers/AlertsControllerTests.cs

## Test / Build Results Already Recorded
- Build (implementation stage): `dotnet build AlertService.sln` -> NOT_CONFIGURED in execution environment (`dotnet` CLI unavailable in that run context).
- Unit tests (scoped):
  - `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"` -> Passed (58/58)
  - `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj` -> Passed (34/34)
  - Scoped total: 92/92 passed
- Coverage:
  - API scoped coverage collected (`coverage.cobertura.xml` generated)
  - SQL coverage collector: NOT_CONFIGURED (`XPlat Code Coverage` collector missing)

## Configuration / Database / Migration Impacts
- Configuration: None
- Database schema/migrations: None

## Known Risks / Limitations
- Unrelated pre-existing API test failure exists in full broad suite (`HealthChecksTests.Live_ReturnsHealthyWithoutTouchingSql`, logger freeze); this is outside ALERT-412 scope and did not affect scoped AC validation.
