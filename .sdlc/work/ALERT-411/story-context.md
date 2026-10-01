# Story Context — ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** NOT_AVAILABLE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Suppress near-duplicate alert creation for active alerts by title+severity within a configurable time window.

## Acceptance Criteria / Bug Behavior / Test Target
- On `POST /api/alerts`, when an active alert with same `Title` (case-insensitive) and same `Severity` was created within the configured duplicate window, do not create a new row.
- Return `200 OK` with the existing alert payload and response header `X-Duplicate-Suppressed: true` when suppression occurs.
- Return `201 Created` for genuinely new alerts.
- Suppression window (minutes) must be configurable in `appsettings.json`, not hardcoded.
- Do not suppress across different severities.
- Do not suppress when prior matching alert is inactive.

## Work Relationship
- standalone

## Constraints
- Duplicate comparison must be case-insensitive on title and exact match on severity.
- Duplicate detection should use active alerts only and a "created within last N minutes" boundary.
- API contract must preserve existing create behavior for non-duplicates (`CreatedAtRoute` + 201).
- Config default must be explicit and safe if missing/invalid (`TO_BE_CONFIGURED` handling if no existing policy pattern is discovered).

## Selected Standards
- `coding`
- `backend-dotnet`
- `api-rest`
- `database`
- `service-architecture`

## Unresolved Questions
- None.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
