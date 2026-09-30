# Story Context — ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** ALERT (Jira)
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Operators can attach multiple free-form tags to an alert and filter alerts by tag. Introduces a
new `Tag` concept with a many-to-many relationship to `Alert`, tag add/remove endpoints, an
optional `tag` filter on the alert list, and tags in the alert response.

## Acceptance Criteria / Bug Behavior / Test Target
- New `Tag` concept, many-to-many with `Alert` (new join table + EF migration).
- `POST /api/alerts/{id}/tags` adds one or more tags: dedupe case-insensitively, max 10 tags/alert, each tag 1–30 chars.
- `DELETE /api/alerts/{id}/tags/{tag}` removes a tag; 404 if the alert or tag assignment is missing.
- `GET /api/alerts` gains an optional `tag` filter, composable with `isActive`, `severity`, date range, `search`.
- Tags are included in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- See `implementation-cache.json` `constraints` for the authoritative list (tag limits, join-table
  design decision, response shape, status-code rules). Not duplicated here.

## Selected Standards
- Owned by `implementation-cache.json` `selected_standards` (coding, backend-dotnet, api-rest,
  service-architecture, database).

## Design Decisions / Assumptions (conservative defaults, no blocking ambiguity)
- `AlertResponse.Tags` = ordered `List<string>` of tag names.
- Explicit `AlertTag` join entity (`AlertTags` table) with composite PK and FKs; unique `IX_Tags_Name`.
- Tag name stored with first-seen casing; all matching/dedupe is case-insensitive.

## Unresolved Questions
- None (design decisions above resolved with conservative, standards-aligned defaults).

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
