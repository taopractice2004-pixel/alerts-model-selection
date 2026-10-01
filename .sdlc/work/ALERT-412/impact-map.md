# Impact Map — ALERT-412

## Primary Slice
- `AlertService.API/Controllers/AlertsController.cs` — new `GET /api/alerts/trends` action
- `AlertService.API/Services/IAlertService.cs` + `AlertManagementService.cs` — new
  `GetTrendsAsync` contract + zero-fill/bucketing implementation
- `AlertService.Data/Interfaces/IAlertRepository.cs` + `AlertService.Data.SQL/Repositories/AlertRepository.cs`
  — new `GetTrendsAsync` read-only aggregation query
- `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs`,
  `AlertService.DTO/Responses/AlertTrendsResponse.cs`, `AlertTrendBucketResponse.cs` — new DTOs
- `AlertService.Common/Constants/AlertConstants.cs` — new `days` range/default constants

## Adjacent Dependencies
- `AlertService.DTO/Responses/AlertSeverityCountsResponse.cs` — reused unchanged for the
  per-bucket severity breakdown
- `AlertService.Common/Enums/Severity.cs` — reused unchanged for severity ordering
  (Low, Medium, High, Critical)
- `AlertManagementService`'s existing injected `TimeProvider` — reused for the "today" anchor

## Out Of Scope
- `GET /api/alerts/summary` — unchanged; only its severity-ordering/shape convention is reused
- `AlertService.Models/Alert.cs`, `AlertDbContext`, EF migrations — no entity/schema change;
  this story only adds a read query over existing `CreatedDate`/`Severity` columns
- Alert CRUD endpoints (`GetById`, `Create`, `Update`, `Deactivate`, `Delete`, tag endpoints) —
  unaffected
- Timezone/locale handling beyond UTC calendar days

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
