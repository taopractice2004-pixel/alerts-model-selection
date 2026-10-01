# Story Context — ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Operators can attach multiple free-form tags to an alert and filter alerts by tag. Introduces a
first-class `Tag` entity with a many-to-many relationship to `Alert`, add/remove tag endpoints,
a composable `tag` query filter on alert listing, and tags surfaced in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- New `Tag` concept with many-to-many to `Alert` via a new join table + EF migration.
- `POST /api/alerts/{id}/tags` adds one or more tags; case-insensitive dedup; max 10 per alert;
  each tag 1-30 chars.
- `DELETE /api/alerts/{id}/tags/{tag}` removes a tag; 404 if the alert or the tag assignment is
  missing.
- `GET /api/alerts` gains an optional `tag` filter composable with existing filters (isActive,
  severity, date range, search) while preserving paging/sorting.
- Tags included in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- `Tag` is a first-class entity (Id, Name) with a unique index on normalized Name; many-to-many
  to `Alert` via an EF-managed join table (`AlertTags`).
- Case-insensitive normalization/dedup (store a single canonical representation); trim input and
  reject empties.
- Max 10 tags per alert enforced in the service after dedup against existing assignments; each
  tag 1-30 chars after trim.
- DELETE returns 404 for both alert-missing and assignment-missing cases.
- `GetByIdAsync`/`GetAllAsync` must `Include` Tags so `AlertResponse.Tags` is populated (watch
  `AsNoTracking` + `Include`).
- Follow existing conventions: constants in `AlertConstants`, DataAnnotations +
  `IValidatableObject` for cross-field validation, manual mapping extensions, repository is the
  only EF boundary (service must not reference EF Core), enum-as-string columns, UTC datetime2.
- Migrations are authoritative; also update the idempotent SQL mirror
  `database/02_AlertServiceDb_Migrations.sql`.
- Security: validate tag input at the trust boundary; use parameterized EF LINQ (no injection);
  no secrets; error handling must not leak internals.

## Selected Standards
- coding
- backend-dotnet
- api-rest
- database

## Unresolved Questions
- None blocking. Canonical tag casing (lowercase vs first-seen) is an implementation detail to
  confirm during implement-story; default to case-insensitive normalization.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
