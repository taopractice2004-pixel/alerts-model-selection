# Changes - ALERT-412

## Implemented
- Added `GET /api/alerts/trends` to `AlertsController` with `[FromQuery] AlertTrendsQueryRequest` so `days` uses the existing `[ApiController]` validation pipeline and returns `ValidationProblemDetails` for invalid query values.
- Added `AlertTrendsQueryRequest` with shared trend-day constants so `Days` defaults to `7` and stays bounded to `1..90`.
- Added `AlertTrendsResponse` and `AlertTrendBucketResponse` DTOs for daily trend buckets with requested-day metadata plus total and per-severity counts.
- Extended `IAlertService` / `AlertManagementService` with trend retrieval that:
  - uses `TimeProvider` UTC time,
  - treats the window as the last `N` UTC calendar days including the current UTC day,
  - returns buckets in oldest-first order,
  - zero-fills missing days and missing severity counts,
  - preserves severity ordering as `Low`, `Medium`, `High`, `Critical`.
- Extended `IAlertRepository` / `AlertRepository` with EF Core daily aggregation over the requested UTC date window using grouped per-day severity counts.

## Tests Updated
- `AlertService.API.Tests\Controllers\AlertsControllerTests.cs`
  - added controller coverage for trends action dispatch and trends query validation/default behavior.
- `AlertService.API.Tests\Services\AlertManagementServiceTests.cs`
  - added service coverage for UTC window calculation, oldest-first ordering, day zero-fill, and severity zero-fill.
- `AlertService.Data.SQL.Tests\Repositories\AlertRepositoryTests.cs`
  - added repository coverage for UTC date-window filtering and per-day per-severity aggregation.

## Validation
- `dotnet test AlertService.API.Tests\AlertService.API.Tests.csproj --no-restore` ✅ Passed
- `dotnet test AlertService.Data.SQL.Tests\AlertService.Data.SQL.Tests.csproj --no-restore` ✅ Passed
- `dotnet build AlertService.sln --no-restore` ✅ Passed

## Notes
- Updated `implementation-cache.json` and `impact-map.md` to capture the shared constants file and DTO files that became part of the implemented slice.
