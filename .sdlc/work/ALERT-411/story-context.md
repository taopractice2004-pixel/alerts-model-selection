# Story Context - ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Suppress near-duplicate active alerts during `POST /api/alerts` when the same title (case-insensitive) and severity were already created within a configurable recent window.

## Acceptance Criteria / Bug Behavior / Test Target
- Return `200 OK` with the existing alert plus response header `X-Duplicate-Suppressed: true` when an active matching alert exists inside the configured window.
- Return `201 Created` for genuinely new alerts.
- Read the suppression window from `appsettings.json`; do not hardcode 15 minutes in implementation.
- Do not suppress across different severities or against inactive prior alerts.

## Work Relationship
- standalone

## Constraints
- Keep the change in the existing controller -> service -> repository -> EF Core slice.
- Reuse `TimeProvider` for time-window evaluation so tests stay deterministic.
- Keep title matching aligned with current trimmed create mapping and case-insensitive repository search style.
- No schema or migration work is expected for this story; suppression is query/behavior only.
- Preserve the current alert response contract; add only the duplicate-suppressed header/status behavior required by the story.

## Selected Standards
- coding
- backend-dotnet
- api-rest
- database
- service-architecture

## Unresolved Questions
- None

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
