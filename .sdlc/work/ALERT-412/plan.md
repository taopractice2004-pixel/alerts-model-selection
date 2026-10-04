# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it.

## Summary
Add `GET /api/alerts/trends?days=N` returning daily alert-creation volume for the last `N` UTC
calendar days (default 7, min 1, max 90, oldest first). Each day bucket carries a total count and
a per-severity breakdown (Low, Medium, High, Critical), with zero-count days and severities
explicitly present. Invalid `days` returns `400` with `ValidationProblemDetails`, consistent with
existing query validation.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC6). Verification notes:
  - AC1/AC6 are verified at the controller layer (default binding, and `400`
    `ValidationProblemDetails` for out-of-range/non-numeric `days`).
  - AC2/AC3/AC4/AC5 are verified at the service layer with a mocked repository and a controlled
    `TimeProvider` (range, ordering, totals, per-severity counts, zero-fill), plus a
    repository-level test for the UTC daily grouping query.

## Change Strategy
- Add `AlertTrendQueryRequest` with an `int Days` property defaulting to 7 and `[Range(1,90)]`,
  mirroring `AlertQueryRequest`; `[ApiController]` turns binding/range failures into
  `ValidationProblemDetails` automatically.
- Add a repository method returning per-UTC-day, per-severity raw counts over `Alert.CreatedDate`
  (grouped by date), reusing the existing `AsNoTracking`/`GroupBy` aggregation style.
- In the service, compute the UTC window from the injected `TimeProvider`, then build one bucket
  per day across the full range and zero-fill missing days/severities (mirroring how
  `GetSummaryAsync` assembles its response).
- Add `AlertsController.GetTrends([FromQuery] AlertTrendQueryRequest)` returning `200 OK` with the
  trend response, mirroring the existing `summary` endpoint.
- Reuse `AlertSeverityCountsResponse` for per-bucket severity counts; add new `AlertTrendResponse`
  (envelope) and `AlertTrendBucketResponse` (per-day) response types.

## Validation Strategy
- Implementation: build only — see `work.json` → `build_commands`
  (`dotnet build AlertService.sln` compiles the touched API, DTO, Data, and Data.SQL projects).
- Unit testing: service tests with mocked `IAlertRepository` + fake `TimeProvider` cover
  AC2–AC5 and zero-fill; controller tests cover AC1 (default 7) and AC6 (`400`
  `ValidationProblemDetails` boundaries); a repository test covers the UTC daily grouping.

## Boundaries
- Primary slice: new `GET /api/alerts/trends` across controller → service → repository, plus the
  new trend request/response DTOs and day-range constants.
- Out of scope: existing endpoints, DTO request contract changes for other endpoints, database
  schema/migration changes, and any non-severity dimension or timezone other than UTC.

## Requirement Analysis
Not required.
