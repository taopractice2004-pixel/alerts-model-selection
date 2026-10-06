# Plan — ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Suppress near-duplicate alerts on `POST /api/alerts`: return the existing active alert (200 +
`X-Duplicate-Suppressed: true`) when same Title (case-insensitive) and Severity was created within a
configurable window; otherwise create as before (201).

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC7). Verified in `/unit-testing` via service tests
  (suppress / different severity / inactive / outside window / config-driven window), controller tests
  (200 + header vs 201), and repository tests (lookup filters).

## Change Strategy
- Add `IAlertRepository` lookup for the most recent active alert matching Title (ci) + Severity with
  `CreatedDate >= now - window`.
- `AlertManagementService.CreateAsync` checks the window (from `IOptions<DuplicateSuppressionOptions>`,
  bound in `Program.cs`, default in `appsettings.json`) before inserting; returns a result carrying the
  alert and a `DuplicateSuppressed` flag.
- Controller maps the flag to `200 OK` + header or `201 Created`.

## Validation Strategy
- Implementation: build only (`dotnet build AlertService.API`) compiles the API plus referenced Data/Data.SQL.
- Unit testing: as listed above; existing `CreateAsync` service/controller tests updated for the new return type.

## Boundaries
- Primary slice: `POST /api/alerts` create path.
- Out of scope: schema/migrations, update/deactivate flows, tagging, cross-instance locking.

## Requirement Analysis
- Risks: concurrent identical requests may both insert (best-effort suppression); `CreateAsync` return-type change touches existing tests.
- Resolution needed from human: confirm assumptions in `work.json` → `missing_facts` / `unresolved_questions` (config key name, inclusive boundary, most-recent match, 0 disables) before or during implementation.
