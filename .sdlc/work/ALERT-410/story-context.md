# Story Context — ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** NOT_AVAILABLE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add alert tagging with persistence, assignment endpoints, query filtering by tag, and response projection of tags.

## Acceptance Criteria / Bug Behavior / Test Target
- Add new Tag concept with Alert many-to-many persistence and migration.
- Add add/remove tag endpoints with deduplication and not-found behavior.
- Add optional tag filter to existing alerts query, composable with current filters.
- Include tags in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- Tag deduplication must be case-insensitive.
- Maximum 10 tags per alert.
- Tag length must be 1-30 characters.
- Keep existing `/api/alerts` filters composable (no regression to isActive/severity/date/search).

## Selected Standards
- `coding`
- `backend-dotnet`
- `api-rest`
- `database`
- `service-architecture`

## Unresolved Questions
- None.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
