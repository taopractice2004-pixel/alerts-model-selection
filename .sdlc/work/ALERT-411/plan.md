# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Suppress near-duplicate alert creation requests so operators receive the existing active alert instead of a second row when the same issue is reposted inside a configurable window.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should verify suppressed-create behavior, non-suppressed create behavior, and the active/severity/window boundaries.

## Change Strategy
- Add a narrow duplicate-check path to alert creation so the service can decide between returning an existing active alert and persisting a new one.
- Keep HTTP concerns in the controller by surfacing enough create-result information through the alert service contract to choose `200 OK` plus the suppression header versus the existing `201 Created` response.
- Read the suppression window from existing application configuration so the default story value lives in `appsettings.json` and not in the service logic.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) because the change crosses the API, service, and SQL persistence projects.
- Unit testing: targeted controller, service, and repository tests should prove suppression decisions, response semantics, and the configurable window behavior for each acceptance criterion.

## Boundaries
- Primary slice: alert creation flow for `POST /api/alerts`.
- Out of scope: alert update/deactivate flows, bulk deduplication of existing data, and changes to non-alert endpoints.

## Requirement Analysis
Not required.