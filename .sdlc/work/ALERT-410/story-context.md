# Story Context — ALERT-410

**Work Item ID:** ALERT-410
**Tracker / Project:** NOT_PROVIDED
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add free-form alert tagging: a new `Tag` concept many-to-many with `Alert`, endpoints to add/remove
tags on an alert, a `tag` filter on `GET /api/alerts`, and `Tags` included in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- New `Tag` concept, many-to-many with `Alert` (new join table/migration).
- `POST /api/alerts/{id}/tags` adds one or more tags: dedupe case-insensitively, max 10 tags/alert,
  each tag 1–30 chars.
- `DELETE /api/alerts/{id}/tags/{tag}` removes a tag; 404 if the alert or tag assignment doesn't exist.
- `GET /api/alerts` gains an optional `tag` filter, composable with `isActive`, `severity`, date range,
  `search`.
- Tags are included in `AlertResponse`.

## Work Relationship
- standalone

## Constraints
- Follow existing layering: Controller → Service (`IAlertService`) → `IAlertRepository` → EF Core
  (`AlertRepository`/`AlertDbContext`). No AutoMapper — extend manual mapping in
  `AlertMappingExtensions.cs`.
- New tag length/count limits belong in `AlertService.Common/Constants/AlertConstants.cs`, alongside
  existing shared limits.
- Follow existing 404 convention: service methods return nullable/result indicating not-found, mapped
  to 404 by the controller (see `UpdateAsync`/`DeactivateAsync`/`DeleteAsync`).
- New join table/migration must follow `database-standards.md` naming (`PK_`, `FK_`, `IX_`, 3NF).
- No custom-exception-to-4xx mapping convention exists today; `ExceptionHandlingMiddleware` only maps
  unhandled exceptions to 500. All current 400s come from `[ApiController]` DataAnnotations validation.

## Selected Standards
- coding-standards.md
- backend-dotnet-standards.md
- api-rest-standards.md
- service-architecture-standards.md
- database-standards.md

## Unresolved Questions
- Exact HTTP status code for successful `POST /api/alerts/{id}/tags` (200 vs 201) — left to
  `/implement-story`, guided by `api-rest-standards.md`.
- How to surface the dynamic "max 10 tags/alert" business-rule violation as a 400 given no existing
  custom-exception-to-4xx convention — left to `/implement-story` as an implementation-time design
  decision (e.g. a result object/enum returned by the service, mapped by the controller).

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
	`implementation-cache.json`.
