# Story Context - ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** ALERT
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add a dashboard-focused trend endpoint that returns daily alert-creation counts for the last `N` UTC calendar days, broken down by severity and including zero-count days.

## Acceptance Criteria / Bug Behavior / Test Target
- Add `GET /api/alerts/trends?days=N` under the existing alerts controller.
- Default `days` to `7`; allow only `1..90`.
- Return one bucket per UTC calendar day for the last `N` days, ordered oldest first.
- Each bucket includes total count plus per-severity counts.
- Zero-count days and zero-count severities must still be present in the response.
- Reuse the summary endpoint severity ordering convention.
- Invalid `days` values must return `400` with `ValidationProblemDetails` through the existing query validation path.

## Work Relationship
- standalone

## Constraints
- Preserve the existing controller -> service -> repository layering.
- Prefer a dedicated trends query DTO instead of widening unrelated request models.
- Keep the response contract distinct from the existing summary response while preserving the same severity ordering.
- Use UTC calendar dates only; timezone customization is out of scope.

## Selected Standards
- standards/coding-standards.md
- standards/backend-dotnet-standards.md
- standards/api-rest-standards.md
- standards/service-architecture-standards.md
- standards/database-standards.md

## Unresolved Questions
- None.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.