# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add `GET /api/alerts/trends?days=N`: per-UTC-day alert-creation counts with per-severity breakdown, zero-filled, oldest first.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`. Service tests (fixed `TimeProvider`, mocked repository) prove buckets, ordering, zero-fill and window; request-DTO validation tests prove the range; repository tests prove day/severity grouping and window exclusion.

## Change Strategy
- Mirror the summary endpoint through every layer: new repository grouped-count method, new `GetTrendsAsync` in the service (builds the full day list and zero-fills), new `trends` action returning DTOs.
- Validate `days` with a query DTO using `[Range]` and constants, like `AlertQueryRequest`, so `[ApiController]` produces the 400.

## Validation Strategy
- Implementation: build only (`dotnet build` of the API project compiles all referenced layers touched by the change).
- Unit testing: service, controller/DTO validation and repository tests cover AC1-AC9; AC7 may be `NOT_VERIFIABLE` at unit level.

## Boundaries
- Primary slice: `AlertsController` → `AlertManagementService` → `AlertRepository`, plus 3 new DTO files and 3 constants.
- Out of scope: schema/index changes, caching, filtering by active state or severity, README changes unless documented during implementation.

## Requirement Analysis
Not required
