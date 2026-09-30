# Implementation Plan — ALERT-412

## Change Strategy
- Add a new `AlertTrendQueryRequest` DTO (`int Days = 7` with `[Range(1,90)]`) so `[FromQuery]`
  binding returns `400 ValidationProblemDetails` on out-of-range or non-numeric input, mirroring
  `AlertQueryRequest`.
- Add new response DTOs: `AlertTrendResponse` (the ordered day buckets) and `AlertTrendDayResponse`
  (UTC date + total + the existing `AlertSeverityCountsResponse`), reusing the summary severity DTO.
- Add a repository method on `IAlertRepository`/`AlertRepository` that groups alert-creation counts
  by UTC `CreatedDate.Date` and `Severity` over a `[fromUtc, toUtc]` range, server-side, `AsNoTracking`,
  no `SELECT *`, returning raw grouped counts only (no zero-fill in the DB).
- Add `GetTrendsAsync` to `IAlertService`/`AlertManagementService`: compute the reference day from
  the injected `TimeProvider`, build the full contiguous oldest-first `N`-day range, call the
  repository, then zero-fill every missing day and every missing severity into `AlertTrendResponse`.
- Add a `GetTrends` action (`[HttpGet("trends")]`) to `AlertsController` that binds
  `AlertTrendQueryRequest`, calls the service, and declares `ProducesResponseType` for `200` and the
  `ValidationProblemDetails` `400`.

## Validation Strategy
- `dotnet build AlertService.sln` then `dotnet test` — the existing controller, service, and
  repository test projects cover every changed layer, so the full solution test run is the narrowest
  sufficient gate. Add focused unit tests for: valid default (7 buckets), boundary `days` (1 and 90),
  invalid `days` (0, 91, non-numeric → 400), oldest-first ordering, zero-fill of empty days/severities,
  and severity ordering matching the summary endpoint.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Slice spans controller → service → data → DTO; no schema/migration, auth, or UI change. Range
  assembly and zero-fill stay in the service (DB-independent, testable); grouping stays in the repository.
