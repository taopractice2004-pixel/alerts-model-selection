# Impact Map — ALERT-412

## Primary Slice
- Read-only alert-volume trend endpoint: new `AlertsController.GetTrends` (`[HttpGet("trends")]`),
  `IAlertService`/`AlertManagementService.GetTrendsAsync` (range build + zero-fill),
  `IAlertRepository`/`AlertRepository` grouped daily-severity counts, and new request/response DTOs.

## Adjacent Dependencies
- `AlertService.Data/Interfaces/IAlertRepository.cs` (new daily-counts contract consumed by the service).
- `AlertService.DTO/Responses/AlertSeverityCountsResponse.cs` (reused, unchanged, for per-day severity counts).

## Out Of Scope
- No changes to create, update, deactivate, delete, summary, list/filter, or tag endpoints.
- No schema/migration change (uses existing `CreatedDate`, `Severity`); no new column or index required.
- No auth/authorization change; no frontend/UI (solution is backend-only).

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
