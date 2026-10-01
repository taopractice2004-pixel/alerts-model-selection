# Changes — ALERT-412

Added `GET /api/alerts/trends?days=N`: zero-filled, oldest-first daily alert trend buckets
(total + per-severity counts) for the last N UTC calendar days.

## Files Added

- `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs` — new query DTO with
  `[Range(MinTrendsDays, MaxTrendsDays)] int Days = DefaultTrendsDays`.
- `AlertService.DTO/Responses/AlertTrendBucketResponse.cs` — new response DTO: `Date`,
  `TotalCount`, `SeverityCounts` (reuses `AlertSeverityCountsResponse`).
- `AlertService.DTO/Responses/AlertTrendsResponse.cs` — new response DTO wrapping
  `IReadOnlyList<AlertTrendBucketResponse> Buckets`.

## Files Modified

- `AlertService.Common/Constants/AlertConstants.cs` — added `DefaultTrendsDays` (7),
  `MinTrendsDays` (1), `MaxTrendsDays` (90).
- `AlertService.Data/Interfaces/IAlertRepository.cs` — added
  `GetTrendsAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken)` returning raw
  non-zero `(Date, Severity, Count)` rows.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs` — implemented `GetTrendsAsync` via EF
  Core `GroupBy` over `CreatedDate.Date` + `Severity` within the inclusive `[fromDateUtc,
  toDateUtc]` day range (does not filter by `IsActive`), mirroring `GetSummaryAsync`'s
  aggregation style.
- `AlertService.API/Services/IAlertService.cs` — added
  `GetTrendsAsync(AlertTrendsQueryRequest, CancellationToken)`.
- `AlertService.API/Services/AlertManagementService.cs` — implemented `GetTrendsAsync`: computes
  `today` from the injected `TimeProvider` (not `DateTime.UtcNow`), builds one bucket per day
  from `today.AddDays(-(Days-1))` to `today` oldest-first, zero-filling every day and every
  `Severity` value not present in the repository result.
- `AlertService.API/Controllers/AlertsController.cs` — added `[HttpGet("trends")] GetTrends`
  action, thin pass-through to `IAlertService.GetTrendsAsync`, relying on existing
  `[ApiController]` automatic model-state → 400 `ValidationProblemDetails` behavior for invalid
  `days`.

## Test Files Modified

- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` — added tests for
  `GetTrendsAsync`: empty range, day/severity grouping, exclusion of out-of-range alerts, and
  confirms `IsActive` is ignored.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` — added tests for
  `GetTrendsAsync`: default days (7, oldest-first), explicit days window computation, zero-fill
  of missing days/severities, and null-request guard.
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` — added tests for `GetTrends`
  200 happy path and `AlertTrendsQueryRequest` DataAnnotations validation (days=0, days=91
  invalid; default valid).

## Out of Scope (confirmed untouched)

`GET /api/alerts/summary`, Alert CRUD endpoints, entity schema/migrations, timezone/locale
handling beyond UTC, caching.

## Validation

| Command | Result |
|---|---|
| `dotnet tool restore` | PASS — restored `dotnet-ef` 8.0.31 |
| `dotnet restore` | PASS |
| `dotnet build AlertService.sln` | PASS — 0 warnings, 0 errors |
| `dotnet test` | PASS — AlertService.Data.SQL.Tests: 33/33 passed; AlertService.API.Tests: 50/50 passed (83 total, 0 failed) |
