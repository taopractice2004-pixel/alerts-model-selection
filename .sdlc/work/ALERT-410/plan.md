# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Alert Tagging: add a shared `Tag` entity (many-to-many with `Alert`), POST/DELETE tag endpoints on an alert, an optional `tag` filter on `GET /api/alerts`, and `tags` in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC9). Verified in `/unit-testing`: service tests for dedupe / limit / length / 404, repository tests (EF in-memory/SQLite) for the tag filter composed with each existing filter, controller tests for status codes and routes.

## Change Strategy
- Follow existing layering: Models/Common/DTO → `IAlertRepository` + `AlertRepository` + EF config → `AlertManagementService` → `AlertsController`.
- `Tag` unique by name (case-insensitive via the database's default collation / normalized comparison); implicit-or-explicit join table `AlertTags` created by a new EF migration; regenerate `database/02_AlertServiceDb_Migrations.sql`.
- Business rules (trim, case-insensitive dedupe, max 10, length 1–30) in the service; request validation (length, non-empty array) via DTO attributes consistent with existing request DTOs.
- `tag` filter added as one more predicate in `AlertRepository.GetAllAsync`; tags loaded with the alerts (Include) so `AlertResponse.Tags` is populated everywhere.

## Validation Strategy
- Implementation: build only (`dotnet build AlertService.sln` compiles all projects including the migration).
- Unit testing: extend the three existing test files to cover each AC; the migration itself is verified by build plus repository tests against the mapped model.

## Boundaries
- Primary slice: `AlertsController` → `AlertManagementService` → `AlertRepository` / `AlertDbContext`.
- Out of scope: tag management endpoints (list/rename/delete tags globally), orphan-tag cleanup, tag filtering by multiple values, tags on the summary endpoint, authentication.

## Requirement Analysis
<!-- Case: AMBIGUOUS -->
- Risks: join-table/migration correctness and collation-dependent case-insensitivity; list query must not N+1 or break `TotalCount` paging when joining tags; tag value in the DELETE route needs URL-encoding handling.
- Resolution needed from human: confirm the seven assumptions in `work.json` → `unresolved_questions` (notably POST status code and over-limit rejection behavior). `/implement-story` may proceed with the stated defaults if none are changed.
