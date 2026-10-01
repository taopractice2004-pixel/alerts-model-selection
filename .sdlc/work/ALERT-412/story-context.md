# Story Context — ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add a `GET /api/alerts/trends` endpoint returning daily alert-creation counts bucketed by UTC
calendar day and broken down by severity, mirroring the existing `/api/alerts/summary` slice
end-to-end. See `.sdlc/context/repo-profile.md` for stack/layering.

## Acceptance Criteria / Bug Behavior / Test Target
- New `GET /api/alerts/trends?days=N` endpoint.
- `days` defaults to 7; min 1, max 90.
- One bucket per UTC calendar day for the last N days, oldest-first.
- Each bucket has a total count plus per-severity counts, including zero-count days/severities.
- Reuse existing severity enum ordering (Low=1, Medium=2, High=3, Critical=4) from the summary slice.
- Invalid `days` (out-of-range or non-numeric) returns 400 `ValidationProblemDetails` consistent
  with existing query validation.

## Work Relationship
- standalone

## Constraints
- Counts are over all alerts by `CreatedDate` (alert-creation counts), not filtered by `IsActive`.
- Use the already-injected `TimeProvider` (`_timeProvider.GetUtcNow().UtcDateTime`) to compute
  "today (UTC)" for a deterministic, testable window.
- Reuse `AlertSeverityCountsResponse` for per-severity counts, consistent with the summary endpoint.
- Rely on default `[ApiController]` model-binding/`[Range]` 400 behavior; no custom
  `InvalidModelStateResponseFactory`.
- Additive only; do not alter the existing summary slice behavior.

## Selected Standards
- coding, backend-dotnet, api-rest, database, service-architecture

## Unresolved Questions
- None

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
	`implementation-cache.json`.
