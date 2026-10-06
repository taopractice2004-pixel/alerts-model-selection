# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add `GET /api/alerts/trends?days=N` returning zero-filled, oldest-first daily UTC buckets with total and per-severity creation counts.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; verified in `/unit-testing` via service, repository (Sqlite) and DTO validation tests plus a controller mapping test.

## Change Strategy
- New query DTO with `[Range]` (mirrors `AlertQueryRequest`) so `[ApiController]` returns 400 `ValidationProblemDetails` for out-of-range and non-numeric `days`.
- Repository returns grouped DB counts per (UTC day, severity); service builds the full window with zero-fill using `TimeProvider`, reusing `AlertSeverityCountsResponse`.
- Controller action is HTTP-only and delegates to the service.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build` of the API project compiles all referenced projects touched by the change).
- Unit testing: each AC mapped to a test per `coverage_goals` in `work.json`.

## Boundaries
- Primary slice: `AlertsController` → `IAlertService` → `IAlertRepository`.
- Out of scope: schema/migrations, existing summary endpoint behavior, filtering by status/severity.

## Requirement Analysis
Not required
