# Story Context — ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** None (story details supplied directly for this `/analyze-story` invocation)
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add a new `GET /api/alerts/trends?days=N` endpoint returning one bucket per UTC calendar day for
the last N days (oldest-first), each with a total count and a per-severity breakdown, including
zero-count days and zero-count severities. `days` defaults to 7 (min 1, max 90); invalid values
return 400 `ValidationProblemDetails`. See `implementation-cache.json` for the full acceptance
criteria, exact files, and resolved design decisions — not duplicated here.

## Acceptance Criteria / Bug Behavior / Test Target
- New `GET /api/alerts/trends?days=N` endpoint
- `days` defaults to 7, minimum 1, maximum 90
- One bucket per UTC calendar day for the last N days, oldest-first
- Each bucket has a total count and a count per severity, including zero-count days/severities
- Severity ordering reuses the existing summary endpoint's enum ordering convention
- Invalid `days` (out-of-range or non-numeric) returns 400 with `ValidationProblemDetails`,
  consistent with existing query validation

## Work Relationship
- standalone (no existing story folder for this endpoint; sibling in spirit to the existing
  `GET /api/alerts/summary` endpoint, whose conventions this story reuses)

## Constraints
- Reuse `Severity` enum ordering (Low, Medium, High, Critical) and `AlertSeverityCountsResponse`
  shape from the summary endpoint rather than inventing a new convention
- Use the existing `[ApiController]` automatic model-validation-to-`ValidationProblemDetails`
  behavior (same pattern as `AlertQueryRequest.Page`/`PageSize`) rather than custom validation
  plumbing
- Use the already-injected `TimeProvider` in `AlertManagementService`, not `DateTime.UtcNow`

## Selected Standards
- coding, backend-dotnet, api-rest, service-architecture, database

## Unresolved Questions
- None (acceptance criteria are fully specified; remaining items are conservative, repo-consistent
  implementation-shape decisions recorded in `implementation-cache.json` → `design_decisions`)

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
	`implementation-cache.json`.
