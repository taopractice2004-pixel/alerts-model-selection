# Story Context — ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** NOT_AVAILABLE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add a trends read endpoint that returns daily alert-creation counts for the last N UTC calendar
days, including severity breakdowns with zero-filled buckets.

## Acceptance Focus
- New `GET /api/alerts/trends?days=N` endpoint.
- `days` defaults to `7`, minimum `1`, maximum `90`, and invalid query values return `400`
  `ValidationProblemDetails` via existing API query-validation behavior.
- Response contains one bucket per UTC day (oldest first) across the last N days.
- Each bucket includes total count and per-severity counts, including zero-count days/severities.
- Severity ordering must match existing summary conventions (`Low`, `Medium`, `High`, `Critical`).

## Constraints
- Keep trends calculation scoped to alert creation timestamps (`CreatedDate`) using UTC date
  boundaries.
- Reuse existing service/repository layering and controller validation patterns (no custom
  ad-hoc validation pipeline).

## Selected Standards
- `coding`
- `backend-dotnet`
- `api-rest`
- `database`
- `service-architecture`

## Unresolved Questions
- None.

## Cache Reference
- Exact source/test files, scope anchors, and command inventory are owned by
  `implementation-cache.json`.
