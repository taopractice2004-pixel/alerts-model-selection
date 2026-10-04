# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a trends endpoint that returns last-N-day UTC alert creation buckets with per-severity counts and preserves existing validation behavior for invalid query input.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` -> `acceptance_criteria`.
- Verification during `/unit-testing` will assert endpoint shape/order, zero-fill behavior, and 400 ValidationProblemDetails behavior for invalid `days`.

## Change Strategy
- Extend the existing controller/service/repository path used by summary-style reads, adding a focused trends flow without changing existing endpoint contracts.
- Keep severity output aligned with current summary conventions by explicit mapping order in the service/DTO layer.

## Validation Strategy
- Implementation / bug fix: build only (API project build compiles touched API, DTO, data interface, and SQL repository dependencies for this slice).
- Unit testing: targeted controller/service/repository tests prove each acceptance criterion, including defaults, range validation, non-numeric binding failure, UTC bucket generation, ordering, and zero-fills.

## Boundaries
- Primary slice: alerts read endpoints and aggregation path (controller -> service -> repository).
- Out of scope: existing alert CRUD/filtering endpoints, persistence schema changes, and summary endpoint behavior.

## Requirement Analysis
Not required.
