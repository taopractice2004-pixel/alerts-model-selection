# Story Context - ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** ALERT
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add alert tagging as a bounded extension of the existing alerts API by introducing a tag many-to-many relationship, supporting tag add/remove operations, allowing optional tag filtering on alert list queries, and returning tags in alert responses.

## Acceptance Criteria / Bug Behavior / Test Target
- Add a new `Tag` concept with a many-to-many relationship to `Alert`, including a migration.
- Add `POST /api/alerts/{id}/tags` to assign one or more tags with case-insensitive dedupe, a maximum of 10 tags per alert, and 1-30 character tag length validation.
- Add `DELETE /api/alerts/{id}/tags/{tag}` to remove a tag assignment and return 404 when the alert or assignment does not exist.
- Extend `GET /api/alerts` with an optional `tag` filter that composes with the existing filters.
- Include tags in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- Preserve the existing controller -> service -> repository layering.
- Keep tag filtering composable with `isActive`, `severity`, date range, paging, sorting, and `search`.
- Apply API, service, and database standards without widening into standalone tag management.

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
  `implementation-cache.json`.# ALERT-410 Story Context

- Story: `Alert Tagging`
- User value: operators can attach multiple free-form tags to alerts and filter alert lists by tag.
- In scope: introduce a `Tag` concept with an Alert many-to-many relationship; add `POST /api/alerts/{id}/tags`; add `DELETE /api/alerts/{id}/tags/{tag}`; extend `GET /api/alerts` with an optional tag filter; include tags in `AlertResponse`.
- Business constraints: dedupe tags case-insensitively, maximum 10 tags per alert, each tag length 1-30 characters, and return `404` when the alert or tag assignment does not exist for delete.
- Technical constraints: keep the existing controller -> service -> repository -> EF Core SQL layering, preserve current alert filter composition and response behavior, and implement schema changes through EF Core migration plus any required operational SQL asset updates.
- Main review points: case-insensitive normalization/persistence choice for tags, composable repository filtering with eager loading or projection, and keeping response/mapping changes aligned with new persistence shape.
