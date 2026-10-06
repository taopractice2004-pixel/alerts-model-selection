# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add alert tagging support so alerts can carry multiple free-form tags, expose tag assignment endpoints, filter the alert listing by tag, and include tags in response DTOs.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should prove schema support, tag assignment and removal behavior, tag-filter composition, and response projection.

## Change Strategy
- Extend the alert persistence model with a tag relationship and migration artifacts, then flow tags through the repository, service, controller, and response mapping layers.
- Keep tag normalization and limit enforcement centralized so add/remove/filter behavior stays consistent across API and repository boundaries.
- Follow existing controller conventions for the new endpoints: POST tag assignment returns the updated `AlertResponse`, and DELETE tag removal returns `204 No Content` on success.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.API/AlertService.API.csproj` compiles the API plus its referenced DTO, model, data abstraction, and SQL persistence projects for this slice).
- Unit testing: update the existing controller, service, and repository test suites to verify validation, not-found paths, composable filtering, and returned tags.

## Boundaries
- Primary slice: alert API, service, DTO/model, and SQL repository/schema files identified in `work.json`.
- Supporting slice: shared alert constants and tag-specific request/result/configuration files required to keep validation and persistence behavior consistent.
- Out of scope: standalone tag catalog management, UI changes, and any filtering beyond the single optional `tag` query parameter requested here.

## Requirement Analysis
- Resolved during implementation by following existing controller conventions: POST tag assignment returns the updated `AlertResponse`, and DELETE tag removal returns `204 No Content` on success.