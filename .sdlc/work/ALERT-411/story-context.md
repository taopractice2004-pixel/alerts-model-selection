# Story Context — ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** NOT_PROVIDED
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
On `POST /api/alerts`, suppress creation of a near-duplicate alert when an active alert with the
same `Title` (case-insensitive) and `Severity` was created within a configurable window (default
15 minutes); return the existing alert with `200 OK` and header `X-Duplicate-Suppressed: true`
instead of creating a new row. A genuinely new alert still returns `201 Created`.

## Acceptance Criteria / Bug Behavior / Test Target
- `POST /api/alerts`: if an active alert with the same `Title` (case-insensitive) and `Severity`
  was created within the last N minutes, do not insert a new row.
- On suppression: respond `200 OK` with the existing alert body and response header
  `X-Duplicate-Suppressed: true`.
- On a genuine new alert: respond `201 Created` as today (no header).
- Suppression window (minutes) is configurable via `appsettings.json`, not hardcoded.
- Must NOT suppress across different severities, and must NOT suppress when the prior matching
  alert is inactive (`IsActive == false`).

## Work Relationship
- standalone

## Constraints
- Follow existing layering: Controller → Service (`IAlertService`) → `IAlertRepository` → EF Core
  (`AlertRepository`/`AlertDbContext`). No AutoMapper — reuse existing `AlertMappingExtensions.cs`
  mapping; no new response fields needed (`AlertResponse` is unchanged).
- No `IOptions<T>` pattern exists in the repo today; `Program.cs` reads config directly via
  `IConfiguration.GetValue<T>(...)`. Follow that existing convention for the new
  suppression-window setting rather than introducing an options-pattern first.
- `IAlertService.CreateAsync` currently returns `Task<AlertResponse>` only; it has no way to signal
  "this was a duplicate" back to the controller. This is a genuine repo-convention gap — no
  existing mechanism carries a secondary flag out of a service call. Left as an implementation-time
  design decision (e.g. a small result wrapper), consistent with how ALERT-410 introduced
  `TagLimitExceededException` for its own convention gap.
- Duplicate lookup must be pushed to the repository/EF layer (`IAlertRepository`), not filtered
  in memory, consistent with existing `GetAllAsync` filtering style in `AlertRepository.cs`.
- Time comparisons must use `TimeProvider` (already injected into `AlertManagementService`), not
  `DateTime.UtcNow` directly, consistent with existing `CreateAsync` code.

## Selected Standards
- coding-standards.md
- backend-dotnet-standards.md
- api-rest-standards.md
- service-architecture-standards.md
- database-standards.md

## Unresolved Questions
- Exact shape of the "duplicate signal" returned from `AlertManagementService.CreateAsync` to
  `AlertsController.Create` (e.g. new result record vs. tuple vs. out param) — left to
  `/implement-story`; no existing convention to reuse.
- Config key name/shape for the suppression window (e.g. `Alerts:DuplicateSuppressionWindowMinutes`)
  — left to `/implement-story`, default should be `15` to match the acceptance criteria.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
	`implementation-cache.json`.
