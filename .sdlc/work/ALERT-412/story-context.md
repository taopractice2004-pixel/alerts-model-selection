# Story Context - ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Expose daily alert-creation trend data for the dashboard through a new `GET /api/alerts/trends`
endpoint that returns total and per-severity counts for the last N UTC calendar days.

## Acceptance Criteria / Bug Behavior / Test Target
- Add `GET /api/alerts/trends?days=N`.
- `days` defaults to `7`, with minimum `1` and maximum `90`.
- Return one bucket per UTC calendar day for the last N days in oldest-first order.
- Include zero-count days and zero-count severities in every bucket.
- Keep severity ordering aligned with the existing summary response convention: `Low`, `Medium`,
  `High`, `Critical`.
- Invalid `days` values, including out-of-range and non-numeric values, must return `400` with
  `ValidationProblemDetails` consistent with existing query validation behavior.

## Work Relationship
- standalone

## Constraints
- Keep the change in the existing controller -> service -> repository -> EF Core slice.
- Reuse the repository's existing request-validation pattern by introducing a query DTO instead
  of validating raw strings in the controller.
- Treat the feature as read-only behavior; no schema or migration work is expected.
- Preserve the existing `/api/alerts` and `/api/alerts/summary` behavior while adding the new
  trends surface.
- Use UTC calendar-day semantics consistently from query boundary calculation through response
  bucket labeling.

## Selected Standards
- coding
- backend-dotnet
- api-rest
- database
- service-architecture

## Unresolved Questions
- None

## Cache Reference
- Exact source files, exact test files, validation commands, and planned new DTO files are owned
  by `implementation-cache.json`.
