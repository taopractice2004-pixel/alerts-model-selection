# Plan — ALERT-412

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add a read-only `GET /api/alerts/trends?days=N` endpoint that returns daily alert-creation
counts (total plus per-severity) for the last `N` UTC calendar days, oldest first, with
zero-count days and severities materialized.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC5). Verification note: AC5 is covered by
  driving the controller/request DTO with out-of-range and non-numeric `days` and asserting a
  `400` `ValidationProblemDetails`, mirroring the existing `AlertQueryRequest` validation tests.

## Change Strategy
- Follow the established controller → service → repository flow used by the `summary` endpoint:
  add a `GetTrendsAsync` to `IAlertService` / `AlertManagementService`, backed by a new grouped
  count query on `IAlertRepository` / `AlertRepository`.
- Repository groups by UTC day + severity and returns sparse buckets; the service zero-fills the
  N contiguous days (using the injected `TimeProvider`) and maps per-severity counts onto the
  reused `AlertSeverityCountsResponse` to preserve summary-endpoint ordering.
- Validate `days` through a new `[FromQuery]` request DTO with `[Range(1,90)]` default 7, so
  `[ApiController]` emits `ValidationProblemDetails` automatically.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln` compiles the new
  endpoint, DTOs, service, and repository method across the solution).
- Unit testing: repository tests prove grouped counts and zero-fill; service tests prove default
  days, boundary days, oldest-first ordering, and per-severity mapping; controller tests prove
  the 200 happy path and the 400 `ValidationProblemDetails` for invalid `days`.

## Boundaries
- Primary slice: new trends read path across controller, service, and SQL repository.
- Out of scope: existing CRUD, summary, suppression, and tag behavior; no schema/migration change
  (trends is derived from existing `CreatedDate` + `Severity`).

## Requirement Analysis
<!-- Fill only when work_case is AMBIGUOUS or the work is risky/blocked; otherwise write "Not required". -->
Not required.
