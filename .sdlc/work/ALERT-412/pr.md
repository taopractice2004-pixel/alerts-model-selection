# ALERT-412 — Add alert volume trend endpoint

## PR Title
ALERT-412: Add GET /api/alerts/trends daily volume trend endpoint

## Description
Adds a read-only `GET /api/alerts/trends?days=N` endpoint returning one bucket per UTC
calendar day (oldest first) for the last `N` days, each with a total count and per-severity
counts, including zero-count days and severities.

## Story / Requirement Summary
Add `GET /api/alerts/trends?days=N` returning one bucket per UTC calendar day (oldest first)
for the last N days, each with a total count and per-severity counts, including zero-count days
and severities.

Acceptance criteria:
- **AC1** — `GET /api/alerts/trends` with no `days` parameter defaults to 7 days.
- **AC2** — `days` accepts a minimum of 1 and a maximum of 90; the response contains exactly N
  buckets, one per UTC calendar day for the last N days, ordered oldest first.
- **AC3** — Each daily bucket exposes a total alert-creation count and a count per severity
  (Low, Medium, High, Critical), emitting zero for days and severities with no alerts.
- **AC4** — The per-severity shape and ordering reuse the existing summary endpoint conventions
  (`AlertSeverityCountsResponse`: Low, Medium, High, Critical).
- **AC5** — Invalid `days` (out of the 1-90 range or non-numeric) returns 400 with
  `ValidationProblemDetails`, consistent with existing query validation.

## Implementation Summary
Additive-only change following the established controller → service → repository flow used by
the summary endpoint:
- **Controller** — new `GetTrends` action on `AlertsController` accepting a `[FromQuery]`
  request DTO and returning the trends response.
- **Service** — `GetTrendsAsync` added to `IAlertService` / `AlertManagementService`.
  Zero-fills N contiguous UTC days oldest-first using the injected `TimeProvider` for the
  day-window boundary (no direct `DateTime.UtcNow`), and maps per-severity counts onto the
  reused `AlertSeverityCountsResponse`.
- **Repository** — `GetDailySeverityCountsAsync` added to `IAlertRepository` /
  `AlertRepository`, grouping by UTC day + severity in EF over a half-open window (from
  inclusive, to exclusive) and returning sparse (non-zero) buckets only — no full-table load.
- **DTOs** — new `AlertTrendsQueryRequest` (`[Range(1,90)]`, default 7), `AlertTrendsResponse`
  (ordered daily buckets), `AlertDailyTrendResponse` (Date, TotalCount, SeverityCounts);
  reuses `AlertSeverityCountsResponse`.
- **Constants** — added `DefaultTrendDays` / `MinTrendDays` / `MaxTrendDays` to
  `AlertConstants`.

## Changed-Files Summary
Source: `git status --porcelain` + `git diff --stat HEAD` (read-only).

Production:
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.Common/Constants/AlertConstants.cs`
- `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs` (new)
- `AlertService.DTO/Responses/AlertTrendsResponse.cs` (new)
- `AlertService.DTO/Responses/AlertDailyTrendResponse.cs` (new)

Tests:
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

## Acceptance Criteria → Implementation / Validation Traceability
| AC | Implemented in | Proven by |
|---|---|---|
| AC1 | `AlertTrendsQueryRequest.Days` default 7; `AlertManagementService.GetTrendsAsync` | `AlertsControllerTests.AlertTrendsQueryRequest_DefaultsDaysTo7`; `AlertManagementServiceTests.GetTrendsAsync_WithDefaultRequest_Returns7ContiguousUtcBucketsOldestFirst` |
| AC2 | `AlertTrendsQueryRequest` `[Range(1,90)]`; `GetTrendsAsync` contiguous oldest-first buckets | `GetTrendsAsync_WithMinDays_ReturnsSingleBucketForToday`, `GetTrendsAsync_WithMaxDays_Returns90ContiguousBucketsOldestFirst`, `GetTrendsAsync_WithDefaultRequest_Returns7ContiguousUtcBucketsOldestFirst` |
| AC3 | `GetTrendsAsync` zero-fill of missing days/severities; `GetDailySeverityCountsAsync` grouped counts | `GetTrendsAsync_ZeroFillsMissingDaysAndSeverities_AndSumsTotals`; `AlertRepositoryTests.GetDailySeverityCountsAsync_GroupsByUtcDayAndSeverity_ReturningNonZeroCombosOnly`, `GetDailySeverityCountsAsync_WhenNoAlerts_ReturnsEmpty` |
| AC4 | Reuse of `AlertSeverityCountsResponse` (Low, Medium, High, Critical) in `GetTrendsAsync` | `GetTrendsAsync_ZeroFillsMissingDaysAndSeverities_AndSumsTotals` (per-severity shape/order) |
| AC5 | `[ApiController]` + `[Range(1,90)]` on `AlertTrendsQueryRequest` → `ValidationProblemDetails` | `AlertTrendsQueryRequest_WithDaysOutOfRange_FailsValidation` (0, -1, 91); `AlertTrendsQueryRequest_WithDaysInRange_PassesValidation` (1, 7, 90); non-numeric handled by `[ApiController]` model binding |

Half-open UTC window verified by `GetTrendsAsync_QueriesHalfOpenUtcWindowFromTimeProvider`
(service) and `GetDailySeverityCountsAsync_AppliesHalfOpenWindow_IncludingFromExcludingTo`
(repository).

## Test / Build Results (already recorded)
- Build: `dotnet build AlertService.sln` → succeeded (recorded in `/implement-story`).
- Unit tests: `dotnet test AlertService.API.Tests` → 81/81 passed;
  `dotnet test AlertService.Data.SQL.Tests` → 44/44 passed (125 total). Recorded in
  `/unit-testing`; not re-run here.
- Acceptance criteria: AC1–AC5 all MET.
- Coverage: NOT_CONFIGURED.

## Configuration / Database / Migration Impacts
None. Trends is derived from existing `CreatedDate` + `Severity`; no schema or migration change.

## Known Risks / Limitations
None recorded. Change is additive and read-only; existing CRUD, summary, suppression, and tag
behavior are unchanged.
