# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a trends endpoint for dashboard charting that exposes daily alert-creation totals and per-severity counts over a configurable trailing UTC-day window.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should verify bucket generation for the trailing day window, zero-filled severity/day counts, default and bounded `days` handling, and the `400 ValidationProblemDetails` behavior for invalid query values.

## Change Strategy
- Add a narrow read-only endpoint and query contract for `GET /api/alerts/trends` so the days parameter can use the API's existing model-validation path.
- Extend the alert service and repository aggregate-read path to compute per-day counts and then map them into a response shape that reuses the existing severity ordering conventions.
- Keep zero-fill behavior in the scoped trend-mapping path rather than changing the existing summary endpoint or unrelated alert query flows.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) because the change crosses API, DTO, service, and SQL persistence projects.
- Unit testing: targeted controller, service, and repository tests should prove query validation, trailing-window bucket generation, severity ordering, and zero-filled aggregate results for each acceptance criterion.

## Boundaries
- Primary slice: aggregate alert trend reads for `GET /api/alerts/trends`.
- Out of scope: changes to the existing summary endpoint contract, non-daily time buckets, local-timezone reporting, and dashboard/UI work.

## Requirement Analysis
Not required.