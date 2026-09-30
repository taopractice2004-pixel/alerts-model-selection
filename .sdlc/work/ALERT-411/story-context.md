# Story Context — ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** ALERT (Jira)
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Suppress near-duplicate alerts so operators are not flooded with repeats of the same issue. On
`POST /api/alerts`, an active alert with the same `Title` (case-insensitive) and `Severity`
created within a configurable window (default 15 minutes) is not re-created; the existing alert
is returned with a suppression header. Genuinely new alerts are unaffected.

## Acceptance Criteria / Bug Behavior / Test Target
- Active alert with same `Title` (case-insensitive) and `Severity` created within the window → no new row.
- Suppressed create returns `200 OK` + existing alert + header `X-Duplicate-Suppressed: true`; new alert returns `201 Created`.
- Suppression window (minutes) configurable via `appsettings.json`, not hardcoded.
- Never suppress across different severities, nor when the prior matching alert is inactive.

## Work Relationship
- standalone

## Constraints
- See `implementation-cache.json` `constraints` for the authoritative list (match rule, status
  codes, header behavior, configurable window, repository placement). Not duplicated here.

## Selected Standards
- Owned by `implementation-cache.json` `selected_standards` (coding, backend-dotnet, api-rest,
  service-architecture, database).

## Design Decisions / Assumptions (conservative defaults, no blocking ambiguity)
- Config section `Alerts:DuplicateSuppression:WindowMinutes` bound to `DuplicateSuppressionOptions`; default 15 when absent.
- `IAlertService.CreateAsync` returns a suppression-aware result so the controller sets status + header; DTOs stay entity-free.
- Duplicate lookup added to `IAlertRepository`/`AlertRepository`; EF/LINQ stays out of controller/service logic.
- Window threshold computed via the already-injected `TimeProvider` for deterministic tests.

## Unresolved Questions
- None (acceptance criteria are explicit; defaults resolved conservatively).

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
