# Impact Map - ALERT-412

## Primary Slice
- `AlertService.API\Controllers\AlertsController.cs`
- `AlertService.API\Services\IAlertService.cs`
- `AlertService.API\Services\AlertManagementService.cs`
- `AlertService.Common\Constants\AlertConstants.cs`
- `AlertService.Data\Interfaces\IAlertRepository.cs`
- `AlertService.Data.SQL\Repositories\AlertRepository.cs`

## Planned New Files
- `AlertService.DTO\Requests\AlertTrendsQueryRequest.cs`
- `AlertService.DTO\Responses\AlertTrendBucketResponse.cs`
- `AlertService.DTO\Responses\AlertTrendsResponse.cs`

## Adjacent Dependencies
- `AlertService.Common\Enums\Severity.cs`
- `AlertService.DTO\Requests\AlertQueryRequest.cs`
- `AlertService.DTO\Requests\AlertTrendsQueryRequest.cs`
- `AlertService.DTO\Responses\AlertSummaryResponse.cs`
- `AlertService.DTO\Responses\AlertSeverityCountsResponse.cs`
- `AlertService.DTO\Responses\AlertTrendBucketResponse.cs`
- `AlertService.DTO\Responses\AlertTrendsResponse.cs`
- `AlertService.API\Program.cs`

## Out Of Scope
- Changes to existing summary totals, search/filter behavior, or non-trend endpoints beyond
  compile-time ripple from the new contract.
- User-local timezone bucketing or chart formatting concerns.
- Schema, migration, or seed-data redesign.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
