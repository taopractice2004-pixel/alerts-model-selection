# Changes — ALERT-412

## Stage Summary
- Timestamp: 2026-09-30
- Command: /implement-story
- Files changed:
  - AlertService.DTO/Requests/AlertTrendQueryRequest.cs (NEW — `Days` [Range(1,90)] default 7)
  - AlertService.DTO/Responses/AlertTrendResponse.cs (NEW — oldest-first `Days` bucket list)
  - AlertService.DTO/Responses/AlertTrendDayResponse.cs (NEW — `DateOnly` Date + TotalCount + reused AlertSeverityCountsResponse)
  - AlertService.Data/Interfaces/IAlertRepository.cs (new `GetDailyCountsBySeverityAsync` contract)
  - AlertService.Data.SQL/Repositories/AlertRepository.cs (server-side GroupBy on `CreatedDate.Date` + `Severity`, AsNoTracking, half-open range)
  - AlertService.API/Services/IAlertService.cs (new `GetTrendsAsync` contract)
  - AlertService.API/Services/AlertManagementService.cs (TimeProvider reference day, build oldest-first N-day range, zero-fill missing days/severities)
  - AlertService.API/Controllers/AlertsController.cs (new `GetTrends` action; ProducesResponseType 200 + ValidationProblemDetails 400)
  - AlertService.API.Tests/Controllers/AlertsControllerTests.cs (GetTrends + Days validation tests)
  - AlertService.API.Tests/Services/AlertManagementServiceTests.cs (range/ordering/zero-fill/mapping tests)
  - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs (grouped daily-severity counts tests)
- Description: Added read-only `GET /api/alerts/trends?days=N` returning exactly N UTC calendar-day
  buckets oldest-first (last N days including today), each with a total alert-creation count and a
  per-severity breakdown (Low/Medium/High/Critical) including zero-count days and severities.
  Reference "today" comes from injected `TimeProvider`; DB grouping stays in the repository and
  contiguous-range zero-fill assembly stays in the service. Invalid `days` (out of range /
  non-numeric) yields 400 ValidationProblemDetails via DataAnnotations `[Range]` + model binding.
  No schema/migration change; backend-only.

## Validation Results
- `dotnet build AlertService.sln` → Build succeeded (0 warnings, 0 errors).
- `dotnet test` → 104 total, 104 succeeded, 0 failed, 0 skipped. (Pre-existing health-check
  Unhealthy log line is expected test behavior, unrelated to this slice.)

## Coverage Results
- NOT_RUN (focused build + test validation used per cache; coverage command available as
  `dotnet test --collect:"XPlat Code Coverage"` if required).

## Reproduction Results
- NOT_RUN (story, not a bug — no reproduction command in cache).

## Standards Applied
- coding-standards.md, backend-dotnet-standards.md, api-rest-standards.md,
  service-architecture-standards.md, database-standards.md — EF/LINQ confined to the repository,
  DTO-only controller surface, deterministic time via TimeProvider, AsNoTracking read query.

## Deferred Items
- None.
