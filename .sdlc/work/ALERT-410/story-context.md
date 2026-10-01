# Story Context - ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add alert-scoped tag assignment, removal, filtering, and response projection within the existing alert API -> service -> repository -> EF Core slice.

## Acceptance Criteria / Bug Behavior / Test Target
- Add a new `Tag` concept with a many-to-many relationship to `Alert` backed by a migration and join table.
- Add `POST /api/alerts/{id}/tags` to add one or more tags with case-insensitive deduplication, 1-30 character tag validation, and a 10-tag maximum per alert.
- Add `DELETE /api/alerts/{id}/tags/{tag}` to remove a tag assignment and return `404` when the alert or assignment does not exist.
- Extend `GET /api/alerts` with an optional tag filter that composes with existing filters.
- Include tags in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- Reuse `DataAnnotations` and shared constants for request validation.
- Keep the change inside the existing alert slice and avoid standalone tag-management features.
- Use case-insensitive comparison for deduplication and filtering.
- Preserve existing GET paging, sorting, and total-count behavior when tag filtering is applied.
- Assume `POST /api/alerts/{id}/tags` uses a wrapper request body containing a tag collection, `GET /api/alerts` uses a single optional `tag` query value, comparisons are case-insensitive, and deleting the last assignment does not require orphan-tag cleanup.

## Selected Standards
- coding
- backend-dotnet
- api-rest
- database
- service-architecture

## Unresolved Questions
- None

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
