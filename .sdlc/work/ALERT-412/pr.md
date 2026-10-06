# ALERT-412: Add alert volume trend endpoint

## PR Title
ALERT-412: Add GET /api/alerts/trends daily alert volume endpoint

## Description
Adds `GET /api/alerts/trends?days=N` returning one zero-filled bucket per UTC day (oldest first) with total and per-severity counts. `days` defaults to 7 and is validated to 1–90.

## Story / Requirement Summary
See `work.json` → `summary` and `acceptance_criteria` (AC1–AC7).

## Implementation Summary
- New `AlertTrendsQueryRequest` (`[Range(1, MaxTrendDays)]`, default 7 from `AlertConstants`) and `AlertTrendBucketResponse` (`Date` as `DateOnly`, total, reused `AlertSeverityCountsResponse`).
- `AlertsController`: new `[HttpGet("trends")] GetTrends` (200 / 400 `ValidationProblemDetails`); literal route does not collide with `{id:int}`.
- `AlertManagementService.GetTrendsAsync`: uses `TimeProvider` for today (UTC), window = today − (N−1) through today, zero-fills days/severities, orders oldest first.
- `AlertRepository.GetCreatedCountsByDayAsync`: database-side grouping by UTC day + severity over `CreatedDate >= startUtc`, `AsNoTracking`, no raw SQL.
- Counts include inactive alerts.

## Changed Files (source: `git status --porcelain`)
Production (8):
- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.Common/Constants/AlertConstants.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.DTO/Requests/AlertTrendsQueryRequest.cs (new)
- AlertService.DTO/Responses/AlertTrendBucketResponse.cs (new)

Tests (3):
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

## Acceptance Criteria Traceability
| AC | Implemented in | Verified by | Result |
|---|---|---|---|
| AC1 | `GetTrends`, `GetTrendsAsync` | Service bucket tests; controller `GetTrends` returns Ok | MET |
| AC2 | `AlertTrendsQueryRequest` default, `GetTrendsAsync` window | Default 7 buckets; days 1/30/90 theory; repository called with UTC start | MET |
| AC3 | `GetTrendsAsync` ordering | Oldest-first, ending-today test | MET |
| AC4 | `GetTrendsAsync` zero-fill | Zero-fill test (total = sum of severities) | MET |
| AC5 | `AlertTrendBucketResponse` reuses `AlertSeverityCountsResponse` | DTO shape in service tests (string enum serialization is global JSON config, not asserted) | MET |
| AC6 | `[Range(1, MaxTrendDays)]` | Validator tests: 0/-1/91 fail, 1/90 pass | MET |
| AC7 | `[ApiController]` model binding | Not unit-testable; needs a `WebApplicationFactory` test | NOT_VERIFIABLE |

## Test / Build Results (already recorded, not re-run)
- Build: `dotnet build AlertService.API/AlertService.API.csproj` succeeded.
- `dotnet test AlertService.API.Tests --filter "FullyQualifiedName~Trend"`: 14/14 passed.
- `dotnet test AlertService.Data.SQL.Tests --filter "FullyQualifiedName~CreatedCountsByDay"`: 3/3 passed.
- Coverage: `GetTrendsAsync` 100% line.
- Test → fix loop: 0/3, `TESTS_PASSED`, no open bugs.

## Configuration / Database / Migration Impacts
None (no schema change or migration; no new dependencies).

## Known Risks / Limitations
- AC7 (non-numeric `days` → 400) relies on `[ApiController]` model binding and has no automated test.
- Response contract assumed: JSON array of `{ date, totalCount, severityCounts }`.
- Window includes today (partial day); counts include inactive alerts.
- `AlertService.API.http` and the `README.md` route table are not updated.
