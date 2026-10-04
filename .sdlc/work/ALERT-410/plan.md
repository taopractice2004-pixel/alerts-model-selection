# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Operators can attach free-form tags to alerts, remove them, filter the alert list by tag, and see tags in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC11). Verified in `/unit-testing`: service tests (dedupe, limit, 404s), controller tests (status codes/routes), repository tests with a relational provider (join table, tag filter composed with other filters).

## Change Strategy
- Add `Tag` entity plus skip-navigation many-to-many (`Alert.Tags` ↔ `Tag.Alerts`), join table via one EF migration; unique index on tag name.
- Extend `IAlertRepository` with a `tag` filter on `GetAllAsync` and tag add/remove operations; always `Include` tags so every `AlertResponse` carries them.
- Service layer owns trimming, case-insensitive dedupe and the 10-tag limit; controller adds the two routes; request-level DataAnnotations enforce 1–30 chars.
- Overflow needs a service-level 400 path (middleware only returns 500) — see unresolved questions.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build` of the API project compiles Models, DTO, Data and Data.SQL; test projects are updated in `/unit-testing`).
- Unit testing: existing Moq-based service tests must be updated for the new repository signature; add tests per AC.

## Boundaries
- Primary slice: `AlertsController` → `AlertManagementService` → `AlertRepository` / EF model.
- Out of scope: tag management endpoints beyond add/remove, tag listing, tag rename, orphan cleanup, frontend, summary endpoint changes.

## Requirement Analysis
- Risks: scope crosses ~19 files (above the usual cap, justified by new entity + migration + 3 layers); signature change breaks existing service tests until `/unit-testing` updates them; `_context.Alerts.Update(alert)` on a graph with loaded tags must not duplicate/recreate Tag rows.
- Resolution needed from human: confirm the assumed defaults listed in `work.json` → `unresolved_questions` (response codes, request body shape, 400 vs 409 on overflow, idempotent duplicates). Proceeding with the defaults is safe if no answer is given.
