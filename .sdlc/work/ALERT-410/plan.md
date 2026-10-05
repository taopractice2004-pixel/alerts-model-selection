# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Alert Tagging: add a many-to-many `Tag` ↔ `Alert` model, add/remove tag endpoints, a composable `tag` filter on `GET /api/alerts`, and tags in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC8). Verified in `/unit-testing`: repository tests (EF InMemory/Sqlite) for persistence, dedupe, limit, filter composition; service tests (Moq) for validation/404 paths; controller tests for status codes.

## Change Strategy
- Add `Tag` entity (unique, case-insensitive name) with implicit skip-navigation many-to-many on `Alert.Tags`; EF config + generated migration `AddAlertTags`; regenerate `database/02_*.sql`.
- Repository: `Include(Tags)` on reads, `tag` predicate `a.Tags.Any(t => t.Name == tag)` (SQL Server default collation is case-insensitive; verify in Sqlite/InMemory tests with explicit normalization), plus add/remove tag operations.
- Service owns tag rules (trim, 1–30 length, case-insensitive dedupe against request and existing, max 10 total, not-found handling); controller exposes `POST /{id:int}/tags` and `DELETE /{id:int}/tags/{tag}` following existing `ProducesResponseType` style.
- `AlertResponse.Tags` (list of strings, empty when none) mapped in `ToResponse`.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln` compiles all projects including migration code).
- Unit testing: each AC mapped to at least one test across the three listed test files; no unit test touches a real SQL Server.

## Boundaries
- Primary slice: `AlertsController` → `AlertManagementService` → `AlertRepository` → new `Tag` model/config/migration.
- Out of scope: tag management endpoints beyond the two specified (list/rename/delete tags), orphan-tag cleanup, tag filter with multiple values, summary endpoint changes, authentication.

## Requirement Analysis
- Assumptions (to be confirmed or accepted by human):
  - `POST` returns `200 OK` with the updated `AlertResponse` (body = `AddAlertTagsRequest { tags: string[] }`); `404` if alert missing; `400` on validation.
  - Tags trimmed; empty/whitespace → 400; >30 chars → 400; resulting total >10 → 400 (whole request rejected, nothing saved). Duplicates (request or existing) silently ignored, not counted against the limit.
  - First-seen casing is stored and returned; matching (add, delete, filter) is case-insensitive.
  - `DELETE` returns `204`; `404` for missing alert or unassigned tag; orphan `Tag` rows kept.
  - `tag` query value is trimmed and length-validated (≤30); blank value is ignored.
- Risks: case-insensitive comparison differs between SQL Server collation and Sqlite/InMemory test providers — normalize explicitly (e.g. store/compare via normalized lower-case key) rather than relying on collation; migration touches the shared snapshot.
- Resolution needed from human: confirm the assumptions above (see `work.json` → `unresolved_questions`), especially POST status code and limit-exceeded status.
