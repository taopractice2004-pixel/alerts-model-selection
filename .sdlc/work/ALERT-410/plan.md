# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add free-form alert tagging with assignment/removal endpoints, composable list filtering, response projection, and the backing tag schema.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should verify the new tag mutation endpoints, GET filtering composition, and response tag projection across API, service, and repository layers.

## Change Strategy
- Extend the existing alert controller, service, repository contract, and EF Core persistence path rather than introducing a separate tagging subsystem.
- Add the smallest tag model/schema surface needed to support many-to-many assignment, case-insensitive matching, and response mapping.

## Validation Strategy
- Implementation / bug fix: build only (`work.json` → `build_commands`) because the implementation stage must only confirm the affected API project and referenced libraries still compile.
- Unit testing: existing controller, service, and repository test suites should cover tag validation, case-insensitive dedupe/removal, and the composed GET filter behavior in isolation.

## Boundaries
- Primary slice: alert API, service orchestration, repository filtering, and EF Core tag persistence for alerts.
- Out of scope: summary aggregation changes, non-alert resources, UI, and broader search semantics beyond the new tag filter.

## Requirement Analysis
Not required.
