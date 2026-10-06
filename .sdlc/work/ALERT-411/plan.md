# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a configurable duplicate-suppression window to the alert create path: a near-duplicate POST
(same Title case-insensitive + same Severity, matching an active alert created inside the window)
returns the existing alert as 200 OK with `X-Duplicate-Suppressed: true` instead of inserting a
new row; genuinely new alerts still return 201 Created.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC6). Verification notes for `/unit-testing`:
  - Service tests (Moq repository + fake `TimeProvider`) prove suppression match logic, window
    boundary, severity mismatch (AC5), and inactive-prior (AC6).
  - Controller test proves 200 + `X-Duplicate-Suppressed: true` on suppression vs 201 Created on
    a new alert (AC2, AC3).
  - A config-bound window (AC4) is proven by driving the service with different window values.

## Change Strategy
- Add a duplicate-lookup method to `IAlertRepository` + its SQL implementation that finds an
  active alert with matching Title (case-insensitive) and Severity created at/after the window
  cutoff; return the most recent match.
- In `AlertManagementService.CreateAsync`, compute the cutoff from injected `TimeProvider` and a
  configuration-bound window; if a match exists, return a result flagged "suppressed" with the
  existing alert; otherwise create as today. Follow the existing `AddTagsResult` result-object
  pattern instead of throwing; update `IAlertService.CreateAsync` return shape accordingly.
- In `AlertsController.Create`, map the result: suppressed → `Ok(existing)` + response header
  `X-Duplicate-Suppressed: true`; created → existing `CreatedAtRoute(...)` 201 path.
- Bind the window from `appsettings.json` (proposed `AlertSuppression:WindowMinutes`, default 15)
  via an options type registered in `Program.cs`; no hardcoded 15.

## Validation Strategy
- Implementation / bug fix: build only — `dotnet build AlertService.sln` compiles all affected
  layers (API, Data interfaces, Data.SQL) and catches the interface/return-shape changes.
- Unit testing: see Acceptance Criteria notes above; `/unit-testing` runs the API and Data.SQL
  test projects in `work.json` → `unit_test_commands`.

## Boundaries
- Primary slice: POST create path across controller → service → repository, plus options wiring.
- Out of scope: GET/PUT/PATCH/DELETE/tags behavior, paging/filtering, DB schema/migrations
  (no new columns required; suppression is a read-before-insert check), and any UI.

## Requirement Analysis
- Risks:
  - Case-insensitive Title matching in SQL must be provider-correct and not rely on tracked
    entities; mirror the existing `AlertRepository` query style (AsNoTracking, `ToLower()`
    comparison) to stay translatable.
  - Changing `IAlertService.CreateAsync`'s return type ripples to the controller and existing
    controller/service tests; keep the change minimal and consistent with `AddTagsResult`.
  - Window boundary semantics (inclusive vs exclusive) and a disabled state when the configured
    value is ≤ 0 must be defined so behavior is deterministic.
- Resolved by human (2026-10-06; recorded in `work.json` → `constraints`, prefixed `DECIDED`):
  - Config key `AlertSuppression:WindowMinutes`, default 15; ≤ 0 disables suppression.
  - Tie-break = most recently created match; incoming `IsActive` does not affect matching (only
    the existing alert must be active).
