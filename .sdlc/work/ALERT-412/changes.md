# Changes — ALERT-412

**Stage:** /implement-story
**Date:** 2026-10-01
**Summary:** Additive `GET /api/alerts/trends` endpoint returning daily alert-creation counts
bucketed per UTC calendar day and broken down by severity. Mirrors the existing
`/api/alerts/summary` slice end-to-end. No existing behavior changed; no new dependencies added.

## Files Changed

| File | Change | Why |
|---|---|---|
| `AlertService.Common/Constants/AlertConstants.cs` | Added `DefaultTrendDays` (7), `MinTrendDays` (1), `MaxTrendDays` (90). | Shared constants for the `days` query param default and `[Range]` bounds. |
| `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs` (NEW) | Non-nullable `int Days` defaulting to `DefaultTrendDays` with `[Range(MinTrendDays, MaxTrendDays)]`. | Non-nullable binding + `[Range]` yields the default `[ApiController]` 400 `ValidationProblemDetails`; no custom factory. |
| `AlertService.DTO/Responses/AlertTrendsResponse.cs` (NEW) | `AlertTrendsResponse` (`Buckets` list) and `AlertTrendBucketResponse` (`Date`, `TotalCount`, `SeverityCounts`). | Response shape reusing `AlertSeverityCountsResponse`, consistent with summary. |
| `AlertService.Data/Interfaces/IAlertRepository.cs` | Added `GetDailyTrendsAsync(fromInclusive, toExclusive, ct)` returning `IReadOnlyList<(DateTime Day, Severity Severity, int Count)>`. | Per-UTC-day severity counts over a date range, mirroring `GetSummaryAsync` style. |
| `AlertService.Data.SQL/Repositories/AlertRepository.cs` | Implemented `GetDailyTrendsAsync` with `AsNoTracking().Where(CreatedDate in [from,to)).GroupBy(CreatedDate.Date, Severity).Count()`. | Follows existing `GetSummaryAsync` GroupBy/Count pattern; window on `CreatedDate`, not filtered by `IsActive`. |
| `AlertService.API/Services/IAlertService.cs` | Added `GetTrendsAsync(AlertTrendsQueryRequest, ct)`. | Service contract for the new slice. |
| `AlertService.API/Services/AlertManagementService.cs` | Implemented `GetTrendsAsync`: UTC window from injected `TimeProvider`, repo call, oldest-first buckets filling zero-count days and severities. Added `AlertService.Common.Enums` using. | Deterministic UTC window; exactly `Days` fully-populated buckets. |
| `AlertService.API/Controllers/AlertsController.cs` | Added `[HttpGet("trends")] GetTrends([FromQuery] AlertTrendsQueryRequest, ct)` with 200 + 400 `ProducesResponseType`. | Mirrors `GetSummary`/`GetAll` controller pattern. |

## Window / Bucket Semantics
- `todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date`
- `fromInclusive = todayUtc.AddDays(-(Days - 1))`, `toExclusive = todayUtc.AddDays(1)`
- Repository groups counts by `CreatedDate.Date` + `Severity` within `[fromInclusive, toExclusive)`.
- Service emits exactly `Days` buckets oldest-first, each with `TotalCount` and a fully-populated
  `SeverityCounts` (0 where absent). Zero-alert days appear with all-zero counts.

## Validation
- `dotnet build` — Build succeeded, 0 Warnings, 0 Errors.
- `dotnet test` — Passed: AlertService.API.Tests 37/37, AlertService.Data.SQL.Tests 25/25 (62 total, 0 failed).

## Notes
- No tests authored in this stage (deferred to the unit-testing stage).
- No new third-party dependencies.
- Touched files, tests, and validation commands match `implementation-cache.json`; no cache correction required.
