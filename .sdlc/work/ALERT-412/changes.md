# Changes - ALERT-412

## Summary
Implemented `GET /api/alerts/trends?days=N` with UTC day bucketing, per-severity daily counts, zero-filled buckets/severities, and validation-aligned 400 behavior for invalid `days` values.

## Source Changes
- `AlertService.API/Controllers/AlertsController.cs`
  - Added `GetTrends` endpoint at `GET /api/alerts/trends`.
  - Added query validation with `[Range(1, 90)]` and default `days = 7`.
- `AlertService.API/Services/IAlertService.cs`
  - Added `GetTrendsAsync(int days = 7, CancellationToken)` contract.
- `AlertService.API/Services/AlertManagementService.cs`
  - Implemented trend shaping logic:
    - Uses UTC `TimeProvider` date.
    - Builds exact `N` day window oldest-first.
    - Requests repository aggregate counts for `[startDay, endDayExclusive)`.
    - Zero-fills missing day/severity combinations.
- `AlertService.Data/Interfaces/IAlertRepository.cs`
  - Added `GetDailySeverityCountsAsync(startUtcInclusive, endUtcExclusive, ...)` contract.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
  - Implemented grouped aggregation by `CreatedDate.Date` and `Severity`.

## Test Changes
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
  - Added trend endpoint controller tests:
    - returns typed `Ok` response
    - passes `days` to service
  - Initialized controller `HttpContext` in test setup to support response header assertions.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
  - Added trend service tests:
    - ordered oldest-first buckets
    - zero-filled missing day/severity buckets
    - out-of-range `days` throws `ArgumentOutOfRangeException`

## Validation
- `dotnet build AlertService.sln` -> passed
- `dotnet test AlertService.API.Tests --filter "FullyQualifiedName~TC_412_|FullyQualifiedName~AlertManagementServiceTests|FullyQualifiedName~AlertsControllerTests"` -> passed (60/60)

## Dependencies
- No new dependencies added.
