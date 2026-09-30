# Story Context - ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** ALERT
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add near-duplicate suppression to `POST /api/alerts` so operators receive an existing active alert instead of a new row when the same title and severity recur within a configurable recent window.

## Acceptance Criteria / Bug Behavior / Test Target
- On `POST /api/alerts`, if an active alert with the same title (case-insensitive) and severity was created within the configured suppression window, do not insert a new row.
- Return `200 OK` with the existing alert and response header `X-Duplicate-Suppressed: true` when suppression occurs.
- Return `201 Created` for genuinely new alerts.
- Read the suppression window from `appsettings.json`; do not hardcode it.
- Do not suppress across different severities or when the prior matching alert is inactive.

## Work Relationship
- standalone

## Constraints
- Preserve the existing controller -> service -> repository layering.
- Keep the controller thin; duplicate-detection policy belongs in the service and persistence lookup belongs in the repository.
- Use the existing `TimeProvider` path to compute the UTC cutoff window.
- No schema or migration work is indicated by this story.

## Selected Standards
- standards/coding-standards.md
- standards/backend-dotnet-standards.md
- standards/api-rest-standards.md
- standards/service-architecture-standards.md
- standards/database-standards.md

## Unresolved Questions
- None.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by `implementation-cache.json`.