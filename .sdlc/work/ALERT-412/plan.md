# Plan - ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` - reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a trend analytics endpoint that returns last-N-day UTC alert creation counts per day with per-severity breakdown, zero-filled buckets, and query validation for `days`.

## Acceptance Criteria / Bug Behavior / Test Target
- Acceptance criteria are owned by `work.json` -> `acceptance_criteria`.
- `/unit-testing` should verify query validation behavior, day-bucket ordering/fill rules, and severity-breakdown ordering consistency.

## Change Strategy
- Extend the existing summary-style controller/service/repository flow with a dedicated trends read path.
- Keep the API contract explicit through a DTO response model that carries day totals and ordered severity counts.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) for compile validation across API/service/data layers.
- Unit testing: run targeted API and SQL repository tests listed in `work.json` to validate AC1-AC3 behavior.

## Boundaries
- Primary slice: alerts analytics read path in controller, service, repository contract, SQL aggregation query, and trends response DTO.
- Out of scope: changes to existing alert create/update/delete behavior and non-trend endpoints.

## Requirement Analysis
Not required.
