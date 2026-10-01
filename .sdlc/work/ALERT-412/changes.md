# Changes — ALERT-412

## Implementation Summary
- Added `GET /api/alerts/trends` in `AlertsController` with `[FromQuery] AlertTrendsQueryRequest`
  and typed `AlertTrendsResponse`, preserving existing `[ApiController]` query-validation behavior.
- Added trends query/response DTOs:
  - `AlertTrendsQueryRequest` (`days` default `7`, range `1..90`)
  - `AlertTrendBucketResponse`
  - `AlertTrendsResponse`
- Extended service/repository contracts and implementations for daily trends aggregation:
  - `IAlertService.GetTrendsAsync(...)`
  - `IAlertRepository.GetDailyTrendsAsync(...)`
  - `AlertManagementService.GetTrendsAsync(...)`
  - `AlertRepository.GetDailyTrendsAsync(...)`
- Added trends constants in `AlertConstants`:
  - `DefaultTrendDays`, `MinTrendDays`, `MaxTrendDays`

## Behavior Implemented
- Uses UTC calendar-day buckets for the last `N` days including today.
- Returns exactly `N` buckets oldest-first.
- Zero-fills missing day buckets and zero-fills severity counts.
- Preserves severity count shape/order (`Low`, `Medium`, `High`, `Critical`).

## Tests Updated
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
  - Added trends controller response test.
  - Added trends query validation tests (range/default).
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
  - Added trends service windowing, ordering, and zero-fill tests.
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`
  - Added repository aggregation tests for UTC day grouping and boundary inclusion/exclusion.

## Validation
1. `dotnet build AlertService.sln` ✅
2. `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"` ✅ (Passed: 53)
3. `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"` ✅ (Passed: 32)
