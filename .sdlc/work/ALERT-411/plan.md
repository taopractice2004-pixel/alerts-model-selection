# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Suppress near-duplicate alerts on `POST /api/alerts`: return the existing alert (200 + `X-Duplicate-Suppressed: true`) instead of inserting, using a configurable window.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`. Verified in `/unit-testing` at service level (mocked repository + fixed `TimeProvider` + `IOptions`), controller level (200 + header vs. 201), and repository level (title/severity/active/window query).

## Change Strategy
- Add an options class bound from `appsettings.json` (default 15 minutes), validated at startup.
- Add one repository query: most recent active alert with same trimmed title (case-insensitive) and severity, created at or after the cutoff.
- `CreateAsync` computes the cutoff from `TimeProvider`, returns a result carrying the alert plus an `IsDuplicate` flag (same pattern as `AddTagsResult`); the controller maps it to 200 + header or 201.
- No schema change or migration.

## Validation Strategy
- Implementation: build only (API project compiles Data and Data.SQL transitively).
- Unit testing: each AC mapped to a service, controller or repository test; existing `Create_*` tests are updated for the new signature.

## Boundaries
- Primary slice: `AlertsController.Create` → `AlertManagementService.CreateAsync` → `IAlertRepository`.
- Out of scope: update/deactivate flows, atomic de-duplication under concurrency, DB constraints, tags.

## Requirement Analysis
Not required
