# Implementation Plan — ALERT-412

## Change Strategy
- Mirror the existing `/api/alerts/summary` slice for a new `trends` endpoint, touching one unit
  per layer: controller action → service(+interface) → repository(+interface) → DTOs → constants.
- Controller: add `[HttpGet("trends")] GetTrends([FromQuery] AlertTrendsQueryRequest, CancellationToken)`
  with the same `ValidationProblemDetails` 400 annotation as the GET-all action.
- Request DTO `AlertTrendsQueryRequest`: non-nullable `int Days` defaulting to 7 with
  `[Range(MinTrendDays, MaxTrendDays)]`; invalid/non-numeric yields automatic 400.
- Service `GetTrendsAsync`: compute the UTC window from `TimeProvider`, call the repository for
  per-UTC-day severity counts, then build oldest-first buckets filling zero-count days and
  zero-count severities.
- Repository: add a method returning per-UTC-day severity counts over a date range, grouping by
  `CreatedDate.Date` + severity following the existing `GetSummaryAsync` GroupBy/Count pattern.
- Add response types `AlertTrendsResponse` + `AlertTrendBucketResponse` (reusing
  `AlertSeverityCountsResponse`); add `DefaultTrendDays`/`MinTrendDays`/`MaxTrendDays` constants.

## Validation Strategy
- `dotnet build` then `dotnet test` — compilation plus the existing unit suites cover the new
  additive slice; no narrower project-scoped command is configured.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Tests are a later stage; do not author them now.
