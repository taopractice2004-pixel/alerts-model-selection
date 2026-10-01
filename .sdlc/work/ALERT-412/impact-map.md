# Impact Map — ALERT-412

## Primary Slice
- `AlertService.API/Controllers/AlertsController.cs` — new `GET /api/alerts/trends` action.

## Adjacent Dependencies
- `AlertService.API/Services/AlertManagementService.cs` + `IAlertService.cs` — `GetTrendsAsync`.
- `AlertService.Data/Interfaces/IAlertRepository.cs` + `AlertService.Data.SQL/Repositories/AlertRepository.cs`
  — per-UTC-day severity counts over a date range.
- DTOs: `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs`,
  `AlertService.DTO/Responses/AlertTrendsResponse.cs` (+ `AlertTrendBucketResponse`),
  reusing `AlertService.DTO/Responses/AlertSeverityCountsResponse.cs`.
- Constants: `AlertService.Common/Constants/AlertConstants.cs`.
- Severity ordering: `AlertService.Common/Enums/Severity.cs` (reuse, do not change).

## Out Of Scope
- The existing `/api/alerts/summary` slice behavior (reference only).
- `IsActive` filtering, persistence/schema changes, and test authoring (later stage).

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
