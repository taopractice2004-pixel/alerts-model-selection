# Story Context — ALERT-411

**Work Item ID:** ALERT-411
**Tracker / Project:** NONE
**Work Type:** story
**Work Case:** SIMPLE
**Repository Mode:** MODE_C_EXISTING_PROJECT
**Effort Mode:** low

## Summary
Suppress near-duplicate alert creation so operators are not flooded with repeats of the same
issue. On alert creation, if an active alert with the same Title (case-insensitive, trimmed)
and same Severity exists within a configurable recent time window, reuse it instead of inserting
a new row.

## Acceptance Criteria (summary)
- `POST /api/alerts`: when an **active** alert with the same **Title (case-insensitive)** and same
  **Severity** was created within the last N minutes, do not insert a new row — return **200 OK**
  with the existing alert plus response header `X-Duplicate-Suppressed: true`.
- A genuinely new alert still returns **201 Created**.
- The suppression window (minutes) is **configurable via appsettings.json**, not hardcoded.
- Do **not** suppress across different severities, when the prior match is **inactive**, or when
  the prior match is **outside the window**.

## Work Relationship
- standalone

## Constraints / Decisions
See `implementation-cache.json` (authoritative) for the full constraint list and file inventory.
Key anchors:
- Config section `AlertSuppression:WindowMinutes` (default 15) bound to a strongly-typed options
  class and registered in `Program.cs`; window must be read from config, not hardcoded. A code
  fallback default of 15 when config is missing is an acceptable design note.
- Duplicate lookup lives in `IAlertRepository`/`AlertRepository` (EF is the only boundary); the
  service must not reference EF Core. Window math (`now - window`) uses the injected `TimeProvider`
  (UTC); case-insensitive title match reuses the existing EF-translatable `.ToLower()` pattern.
- Service returns a small result (`AlertCreateResult`: `AlertResponse` + `WasSuppressed`) so the
  controller can choose 200+header vs 201.
- No new DB columns → no EF migration / SQL mirror change required.

## Security
- Validate input at the trust boundary (reuse existing request validation on `CreateAsync`).
- Use parameterized EF LINQ for the duplicate query (no injection); no secrets introduced.
- Error handling must not leak internals in responses.

## Selected Standards
- coding
- backend-dotnet
- api-rest
- service-architecture
- database

## Unresolved Questions
- None blocking.

## Cache Reference
- Exact source files, exact test files, validation commands, and scope anchors are owned by
  `implementation-cache.json`.
