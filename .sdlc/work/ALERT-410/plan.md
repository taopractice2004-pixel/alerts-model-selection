# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add alert tagging across the API, service, persistence, and response layers so operators can assign multiple free-form tags and filter alerts by tag.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; `/unit-testing` should verify tag persistence, add/remove endpoint behavior, tag-filter composition, and tag projection in responses.

## Change Strategy
- Extend the alert domain and SQL persistence to support a tag many-to-many association plus an EF Core migration.
- Add narrow alert-tag mutation endpoints while keeping controllers thin and routing business rules through `AlertManagementService` and the repository contract.
- Extend existing query and response DTOs plus mapping so `GET /api/alerts` can filter by tag and `AlertResponse` returns the assigned tags.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) because the change spans API, DTO, models, and EF Core persistence projects.
- Unit testing: targeted controller, service, and repository tests should prove endpoint behavior, validation rules, case-insensitive tag logic, and filter composition for each acceptance criterion.

## Boundaries
- Primary slice: alert API/service/repository/query pipeline and EF Core schema for alert tags.
- Out of scope: tag taxonomy management, bulk tag rename/merge behavior, non-alert endpoints, and UI work.

## Requirement Analysis
- Implemented assumptions: `POST /api/alerts/{id}/tags` accepts a wrapper DTO with a `tags` array, and stored/displayed tag casing preserves the first persisted value while matching and deduping case-insensitively.
- Residual risk: If a raw array contract or canonicalized output casing is required instead, the API contract and related tests will need a follow-up adjustment.