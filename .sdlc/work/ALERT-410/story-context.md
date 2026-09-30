# Story Context - ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** ALERT
**Work Type:** story
**Work Case:** AMBIGUOUS
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add reusable alert tags, assignment/removal endpoints, tag filtering, and tags in alert responses.

## Acceptance Criteria
- Alerts support multiple free-form tags through a many-to-many persistence model.
- POST assigns one or more normalized tags, with case-insensitive deduplication, 1-30 character tags, and at most 10 tags per alert.
- DELETE removes an assignment and returns 404 when the alert or assignment is missing.
- GET filtering by tag composes with existing alert filters and responses include tags.

## Work Relationship
- standalone

## Constraints
- Preserve controller -> service -> repository layering and existing error handling.
- Cross-layer scope is broader than the compact five-file default because the story changes contracts, domain, persistence, migration, and tests.

## Selected Standards
- See `implementation-cache.json`; selected standards are authoritative there.

## Unresolved Questions
- Response representation, duplicate POST behavior, case preservation, and whether the standalone SQL migration script must change are not specified.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by `implementation-cache.json`.
