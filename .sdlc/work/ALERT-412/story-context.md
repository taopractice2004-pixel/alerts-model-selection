# Story Context - ALERT-412

**Work Item ID:** ALERT-412  
**Tracker / Project:** NOT_PROVIDED  
**Work Type:** story  
**Work Case:** AMBIGUOUS  
**Repository Mode:** MODE_C_EXISTING_PROJECT  
**Effort Mode:** low

## Summary
Add a UTC daily alert-volume trend endpoint with severity breakdowns and bounded `days` validation.

## Acceptance Criteria
- `GET /api/alerts/trends?days=N` defaults to 7 and accepts only 1 through 90.
- Return oldest-first UTC calendar-day buckets, including zero-count days and severities.
- Preserve summary severity ordering: Low, Medium, High, Critical.
- Invalid or non-numeric `days` returns 400 with `ValidationProblemDetails`.

## Work Relationship
- standalone

## Constraints
- Preserve controller -> service -> repository layering and async cancellation.
- Use the existing UTC/time-provider and EF/SQL patterns; use set-based, parameterized date-range aggregation.
- Exact inventory and validation commands are authoritative in `implementation-cache.json`.

## Selected Standards
- See `implementation-cache.json` for the five selected standards.

## Unresolved Questions
- Jira project/space was not provided.
- Confirm whether the last N days includes the current UTC day.
- Confirm the trend bucket date field representation; recommended contract is ISO `yyyy-MM-dd`.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by `implementation-cache.json`.
