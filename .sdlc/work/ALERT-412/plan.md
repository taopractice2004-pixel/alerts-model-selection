# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a trends endpoint that reports daily alert-creation volume over the last N UTC days with total and per-severity counts so dashboard consumers can chart recent creation patterns.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should prove the N-day window, chronological bucket ordering, zero-filled days/severities, default and bounded `days` behavior, and the `ValidationProblemDetails` response for invalid query input.

## Change Strategy
- Follow the existing summary-endpoint pattern by adding a focused GET endpoint, service contract, and repository aggregation query rather than embedding reporting logic in the controller.
- Reuse the existing severity-count shape and ordering convention so the trends contract stays consistent with the current dashboard-facing summary DTOs.
- Validate the `days` query parameter through the same ASP.NET Core query-binding path already used elsewhere so out-of-range and non-numeric input fail with the standard API validation payload.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.API/AlertService.API.csproj` compiles the API plus its referenced DTO, service, data abstraction, and SQL repository projects for this slice).
- Unit testing: extend the controller, service, and repository test suites, and add one focused API test if needed, to verify default/range validation, UTC bucket filling, severity ordering, and zero-count behavior.

## Boundaries
- Primary slice: alert trends controller, service, repository contract, repository implementation, new request/response DTO contracts, and the shared alert constants used for the `days` default and range identified in `work.json`.
- Out of scope: changes to existing list/detail/summary endpoints, schema migrations, non-UTC bucketing, custom date-range inputs beyond `days`, and dashboard/UI work.

## Requirement Analysis
Not required.
