# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Suppress near-duplicate alert creates within a configurable time window so operators receive the existing active alert instead of a second row when the same issue is reposted shortly afterward.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should prove the suppressed duplicate path, the normal create path, configuration-driven window behavior, and the inactive/different-severity non-suppression rules.

## Change Strategy
- Add a focused duplicate-detection lookup at the repository seam and let the service decide whether the create request should reuse an existing alert or persist a new one.
- Extend the create flow contract just enough for the controller to distinguish suppressed duplicates from genuine creates so it can set the correct status code and response header.
- Source the suppression-window duration from application configuration rather than hardcoding the 15-minute value in the create path.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.API/AlertService.API.csproj` compiles the API plus its referenced service, data abstraction, and SQL repository projects for this slice).
- Unit testing: update the existing controller, service, and repository test suites to verify header/status behavior, duplicate-window matching, and the inactive/different-severity escape conditions.

## Boundaries
- Primary slice: alert create controller, service, repository contract, repository implementation, and API configuration files identified in `work.json`.
- Out of scope: alert list/detail/query behavior, schema changes, cross-alert dedupe beyond title + severity + active + time-window matching, and any suppression behavior for update/deactivate flows.

## Requirement Analysis
Not required.