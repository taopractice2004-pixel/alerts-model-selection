# Story ALERT-412 - Alert Volume Trend Endpoint

## Story Summary
Add a `GET /api/alerts/trends?days=N` endpoint that returns daily alert-creation counts (total plus a per-severity breakdown) for the last N UTC calendar days, oldest first, including zero-count days and severities, so dashboards can chart trends. | Case: SIMPLE

## Acceptance Criteria
- AC1 - `GET /api/alerts/trends` with no `days` uses a default of 7 days.
- AC2 - The response contains exactly N buckets, one per UTC calendar day for the last N days, ordered oldest first.
- AC3 - Each bucket has a total count and a per-severity count (Low, Medium, High, Critical), with zero-count days and zero-count severities included as 0.
- AC4 - The per-severity breakdown reuses the same severity set and ordering convention as the summary endpoint (Low, Medium, High, Critical).
- AC5 - `days` out of range (< 1 or > 90) returns `400` with `ValidationProblemDetails`, consistent with existing query validation.
- AC6 - A non-numeric `days` value returns `400` with `ValidationProblemDetails`, consistent with existing model-binding behavior.

## In Scope
- New read-only trends endpoint, its query request DTO, response DTOs, service method, and repository aggregation query.
- Reuse of the existing `AlertSeverityCountsResponse` and `Severity` enum ordering from the summary flow.

## Out of Scope
- Any change to existing endpoints, summary logic, paging, or persistence schema.
- Timezone handling other than UTC; caching; new logging sinks.

## Do Not Modify
- `AlertService.Data.SQL/Migrations/**` (EF-generated)
- `database/**` (generated/DBA-owned SQL)
- `standards/**` (company standards)

## Impacted Files

### Files to Modify
| File | Change | ACs |
|---|---|---|
| AlertService.Common/Constants/AlertConstants.cs | Add `DefaultTrendDays = 7`, `MinTrendDays = 1`, `MaxTrendDays = 90` constants | AC1, AC5 |
| AlertService.Data/Interfaces/IAlertRepository.cs | Add `GetDailyCountsAsync(fromInclusiveUtc, toExclusiveUtc, ct)` returning per-day/per-severity counts | AC2, AC3 |
| AlertService.Data.SQL/Repositories/AlertRepository.cs | Implement `GetDailyCountsAsync` with a server-side `GroupBy` on `CreatedDate.Date` + `Severity` | AC2, AC3 |
| AlertService.API/Services/IAlertService.cs | Add `GetTrendsAsync(AlertTrendsQueryRequest, ct)` returning `AlertTrendsResponse` | AC1, AC2, AC3 |
| AlertService.API/Services/AlertManagementService.cs | Implement `GetTrendsAsync`: build N-day calendar (oldest first), fill all severities, zero defaults | AC1, AC2, AC3, AC4 |
| AlertService.API/Controllers/AlertsController.cs | Add `GetTrends([FromQuery] AlertTrendsQueryRequest, ct)` returning `Ok` | AC1, AC5, AC6 |

### Files to Create
| File | Status | Purpose | ACs |
|---|---|---|---|
| AlertService.DTO/Requests/AlertTrendsQueryRequest.cs | TO_CREATE | Query request with `int Days` (default 7, `[Range(1, 90)]`) | AC1, AC5 |
| AlertService.DTO/Responses/AlertTrendBucketResponse.cs | TO_CREATE | One day bucket: `Date`, `TotalCount`, `SeverityCounts` | AC2, AC3 |
| AlertService.DTO/Responses/AlertTrendsResponse.cs | TO_CREATE | Wrapper: `Days` + `IReadOnlyList<AlertTrendBucketResponse> Buckets` | AC2 |

## Reuse / Existing Patterns
| Change | Example file to copy | Reuse instead of creating |
|---|---|---|
| New endpoint | AlertService.API/Controllers/AlertsController.cs (`GetSummary`) | Existing thin-controller + `ProducesResponseType` pattern |
| Query request + validation | AlertService.DTO/Requests/AlertQueryRequest.cs | `[Range]` DataAnnotations + `AlertConstants` constants |
| Service aggregate method | AlertService.API/Services/AlertManagementService.cs (`GetSummaryAsync`) | Build response object directly; reuse injected `TimeProvider` for "today" |
| Repository aggregate query | AlertService.Data.SQL/Repositories/AlertRepository.cs (`GetSummaryAsync`) | `AsNoTracking` + `GroupBy` server-side aggregation |
| Severity breakdown DTO | AlertService.DTO/Responses/AlertSeverityCountsResponse.cs | Reuse as-is (Low/Medium/High/Critical) |

## Implementation Plan

### Contracts
- Endpoint: `GET /api/alerts/trends?days={int}`; `days` optional, default `7`, range `1..90`; responses: `200` `AlertTrendsResponse`, `400` `ValidationProblemDetails`.
- `AlertTrendsQueryRequest`: `int Days { get; set; } = AlertConstants.DefaultTrendDays;` with `[Range(AlertConstants.MinTrendDays, AlertConstants.MaxTrendDays)]`.
- `AlertTrendsResponse`: `int Days`, `IReadOnlyList<AlertTrendBucketResponse> Buckets`.
- `AlertTrendBucketResponse`: `DateTime Date` (UTC calendar day, 00:00:00), `int TotalCount`, `AlertSeverityCountsResponse SeverityCounts`.
- `IAlertService.GetTrendsAsync(AlertTrendsQueryRequest request, CancellationToken) -> Task<AlertTrendsResponse>`.
- `IAlertRepository.GetDailyCountsAsync(DateTime fromInclusiveUtc, DateTime toExclusiveUtc, CancellationToken) -> Task<IReadOnlyList<(DateTime Day, Severity Severity, int Count)>>`.

### Behavior Rules
- When `days` is absent, `Days` defaults to 7 and the response has 7 buckets. (AC1)
- The N-day window is the last N UTC calendar days ending on today (UTC): start = `today.AddDays(-(N-1))`, end-exclusive = `today.AddDays(1)`; "today" from the injected `TimeProvider` (`GetUtcNow().UtcDateTime.Date`). (AC2)
- Buckets are emitted oldest first and there are exactly N of them, one per calendar day. (AC2)
- Repository filters `CreatedDate >= fromInclusiveUtc && CreatedDate < toExclusiveUtc` and groups by `CreatedDate.Date` and `Severity`, returning only non-empty groups; the service fills missing days/severities with 0. (AC2, AC3)
- Each bucket's `SeverityCounts` has all four severities populated (0 when none); `TotalCount` equals the sum of the four severity counts. (AC3)
- Severity set/order mirrors the summary endpoint: Low, Medium, High, Critical. (AC4)
- `days` out of range fails the `[Range]` attribute; `[ApiController]` returns `400` `ValidationProblemDetails`. (AC5)
- A non-numeric `days` fails model binding; `[ApiController]` returns `400` `ValidationProblemDetails`. (AC6)
- Read-only path: `AsNoTracking`, no mutation, no new logging required (consistent with `GetSummaryAsync`).

### Steps
1. [AC1, AC5] AlertService.Common/Constants/AlertConstants.cs -> add `DefaultTrendDays`, `MinTrendDays`, `MaxTrendDays`.
2. [AC1, AC5] AlertService.DTO/Requests/AlertTrendsQueryRequest.cs -> new request with `Days` (default const, `[Range]`); follow AlertQueryRequest.cs.
3. [AC2, AC3] AlertService.DTO/Responses/AlertTrendBucketResponse.cs -> new DTO (`Date`, `TotalCount`, `SeverityCounts`); reuse `AlertSeverityCountsResponse`.
4. [AC2] AlertService.DTO/Responses/AlertTrendsResponse.cs -> new wrapper DTO (`Days`, `Buckets`); follow AlertSummaryResponse.cs.
5. [AC2, AC3] AlertService.Data/Interfaces/IAlertRepository.cs -> add `GetDailyCountsAsync` signature.
6. [AC2, AC3] AlertService.Data.SQL/Repositories/AlertRepository.cs -> implement `GetDailyCountsAsync` (`AsNoTracking`, `Where` on window, `GroupBy` `{ CreatedDate.Date, Severity }`); follow `GetSummaryAsync`.
7. [AC1, AC2, AC3, AC4] AlertService.API/Services/IAlertService.cs + AlertManagementService.cs -> add `GetTrendsAsync`: compute window from `TimeProvider`, call repository, build N oldest-first buckets with all severities filled; follow `GetSummaryAsync`.
8. [AC1, AC5, AC6] AlertService.API/Controllers/AlertsController.cs -> add `GetTrends` action with `[HttpGet("trends")]`, `[ProducesResponseType(typeof(AlertTrendsResponse), 200)]`, `[ProducesResponseType(typeof(ValidationProblemDetails), 400)]`; follow `GetSummary`/`GetAll`.

## Validation Plan
- Build / type check: `AlertService.sln` (Debug) - part of the C# build.
- Lint / static analysis: NOT_CONFIGURED.
- Regression: `AlertManagementServiceTests`, `AlertsControllerTests`, `AlertRepositoryTests` (ensure existing summary/query behavior unchanged).
- Integration / functional: No automated controller suite exists; manual check of `400` for non-numeric/out-of-range `days` and `200` shape (health-check suite is unrelated).
- Coverage: changed/new files in `AlertService.API`, `AlertService.DTO`, `AlertService.Common`, `AlertService.Data.SQL` (tooling present; no numeric target configured).

## Unit Test Plan
| Test file | New or existing | Behaviors and edge cases |
|---|---|---|
| AlertService.API.Tests/Services/AlertManagementServiceTests.cs | EXISTING | `GetTrendsAsync` default 7 buckets when `Days` unset; exactly N buckets oldest-first for a given N; zero-count days and severities filled with 0; `TotalCount` equals sum of severities; buckets map repository day/severity counts correctly (fixed `TimeProvider`) |
| AlertService.API.Tests/Controllers/AlertsControllerTests.cs | EXISTING | `GetTrends` returns `OkObjectResult` with `AlertTrendsResponse`; passes request to service; `AlertTrendsQueryRequest` with `Days` below min / above max fails DataAnnotations validation (mirrors existing `AlertQueryRequest` validation tests) |
| AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs | EXISTING | `GetDailyCountsAsync` groups by UTC day and severity within the window; excludes rows outside the window; returns empty groups omitted (service fills zeros) |

## AC-to-Validation Mapping
| AC | Validation type | Validation (test name and file, or check) | Expected result |
|---|---|---|---|
| AC1 | UNIT_TEST | `GetTrendsAsync_WithoutDays_Returns7Buckets` in AlertManagementServiceTests | 7 buckets returned; `Days = 7` |
| AC2 | UNIT_TEST | `GetTrendsAsync_ReturnsNBucketsOldestFirst` in AlertManagementServiceTests | N buckets, dates ascending, one per UTC day |
| AC3 | UNIT_TEST | `GetTrendsAsync_FillsZeroDaysAndSeverities` in AlertManagementServiceTests | Missing days/severities = 0; `TotalCount` = sum of severities |
| AC4 | UNIT_TEST | `GetTrendsAsync_SeverityBreakdownMatchesSummaryOrdering` in AlertManagementServiceTests | `SeverityCounts` exposes Low/Medium/High/Critical |
| AC5 | UNIT_TEST | `AlertTrendsQueryRequest_WithDaysOutOfRange_FailsValidation` in AlertsControllerTests | Validation fails on `Days`; endpoint annotated for `400` `ValidationProblemDetails` |
| AC6 | INTEGRATION_FUNCTIONAL | Manual: `GET /api/alerts/trends?days=abc` | `400` `ValidationProblemDetails` from `[ApiController]` model binding |

## Dependencies / Risks
- Dependencies: Reuses injected `TimeProvider` (already registered) and `AlertSeverityCountsResponse`. None new.
- Risks: EF Core `CreatedDate.Date` translation and UTC-day bucketing must match between SQL Server (runtime) and the InMemory/Sqlite provider used in repository tests; keep calendar construction in the service layer, not the query.
- Stop points: None (no shared files; no schema or contract changes to existing endpoints).

## Open Questions
- Blocking: None.
- Non-blocking: (1) "Last N days" is assumed to include today (UTC); confirm it should not be "N days ending yesterday". (2) Response is wrapped in `AlertTrendsResponse` (not a raw array) to follow the existing no-raw-array convention; confirm this shape.
- Open decisions (greenfield): None.

## Approval Status
Status: APPROVED
Approved by: developer
Date: 2026-10-02
