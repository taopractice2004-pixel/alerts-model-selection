# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it.

## Summary
Suppress near-duplicate alerts on `POST /api/alerts`: when an active alert with the same Title
(case-insensitive) and same Severity exists within a configurable window (default 15 minutes),
return the existing alert as `200 OK` with header `X-Duplicate-Suppressed: true` instead of
inserting a new row. Non-duplicates keep returning `201 Created`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC6). Verification notes:
  - AC2/AC3 are verified at the controller layer (status code 200 vs 201 and presence/absence of
    the `X-Duplicate-Suppressed` header).
  - AC1/AC4/AC5/AC6 are verified at the service layer with a mocked repository and a controlled
    `TimeProvider`, plus a repository-level test for the duplicate query (window + active +
    severity + case-insensitive title).

## Change Strategy
- Add a bound options type (`AlertSuppressionOptions`, `WindowMinutes`) read from `appsettings.json`
  and registered in `Program.cs`; the service reads the window from options (no hardcoded literal).
- Add a focused repository method (e.g. `FindActiveDuplicateAsync(title, severity, createdAfterUtc)`)
  returning the most recent matching active alert, filtered in the EF query by `IsActive == true`,
  exact `Severity`, case-insensitive `Title`, and `CreatedDate >= threshold`. Reuse the existing
  case-insensitive query style already used in `AlertRepository`.
- In `AlertManagementService.CreateAsync`, compute the threshold from the injected `TimeProvider`
  and the configured window, call the lookup; if a match is found, return the existing alert flagged
  as suppressed, otherwise insert and return the created alert. Signal suppression to the controller
  via a small result shape (follow the existing tuple-status pattern used by `AddTagsAsync`).
- `AlertsController.Create` sets `X-Duplicate-Suppressed: true` and returns `Ok(...)` when suppressed;
  otherwise keeps the current `CreatedAtRoute(...)` `201` path.

## Validation Strategy
- Implementation / bug fix: build only — see `work.json` → `build_commands`
  (`dotnet build AlertService.sln` compiles the touched API, Data, and Data.SQL projects together).
- Unit testing: service tests with mocked `IAlertRepository` + fake `TimeProvider` cover AC1/AC4/AC5/AC6
  and the just-outside-window boundary; controller tests cover AC2/AC3 (status + header); a repository
  test covers the duplicate query semantics.

## Boundaries
- Primary slice: `POST /api/alerts` create path across controller → service → repository, plus the
  suppression-window configuration.
- Out of scope: other endpoints (GET/PUT/PATCH/DELETE/tags), DTO request contract changes, database
  schema/migration changes (no new columns required), and any cross-field dedupe beyond Title+Severity.

## Requirement Analysis
Not required.
