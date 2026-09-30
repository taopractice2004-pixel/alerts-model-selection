# Story Context — ALERT-412

**Work Item ID:** ALERT-412
**Tracker / Project:** ALERT (Jira)
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Add a read-only alert-volume trend endpoint for dashboard charting. `GET /api/alerts/trends?days=N`
returns exactly `N` UTC calendar-day buckets (oldest first) for the last `N` days, each with a
total alert-creation count and a per-severity breakdown, including zero-count days and severities.

## Acceptance Criteria / Bug Behavior / Test Target
- `GET /api/alerts/trends?days=N` (default 7, min 1, max 90) returns one bucket per UTC calendar day for the last `N` days, oldest first.
- Each bucket has a total count plus a per-severity count (Low/Medium/High/Critical), including zero-count days and severities.
- Reuses the summary endpoint's severity enum ordering conventions.
- Invalid `days` (out of range or non-numeric) returns `400` with `ValidationProblemDetails`, consistent with existing query validation.

## Work Relationship
- standalone

## Constraints
- See `implementation-cache.json` `constraints` for the authoritative list (days validation,
  N oldest-first UTC buckets, TimeProvider reference day, service-side zero-fill, repository-side
  grouping, counting basis). Not duplicated here.

## Selected Standards
- Owned by `implementation-cache.json` `selected_standards` (coding, backend-dotnet, api-rest,
  service-architecture, database).

## Design Decisions / Assumptions (conservative defaults, no blocking ambiguity)
- `days` binds via a new `AlertTrendQueryRequest` (`int Days = 7`, `[Range(1,90)]`, `[FromQuery]`);
  non-numeric/out-of-range binding yields `400 ValidationProblemDetails` (mirrors `AlertQueryRequest`).
- Window = UTC today back through today-(N-1) inclusive; reference "today" comes from the injected
  `TimeProvider.GetUtcNow()`, never `DateTime.UtcNow`, for deterministic tests.
- Bucketing by `CreatedDate.Date` (stored UTC); counting basis is creation volume regardless of `IsActive`.
- Repository does server-side `GroupBy` on date + severity (`AsNoTracking`, no `SELECT *`) and returns raw
  grouped counts; the service builds the contiguous oldest-first range and zero-fills missing days/severities.
- Per-day severity counts reuse the existing `AlertSeverityCountsResponse` DTO in enum order.

## Unresolved Questions
- None (acceptance criteria are explicit; defaults resolved conservatively).

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
