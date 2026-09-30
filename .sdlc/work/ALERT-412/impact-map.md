Impact Map — ALERT-412

Planned file changes (minimal useful set):
- `AlertService.API/Controllers/AlertsController.cs` — add `GetTrends` action `GET api/alerts/trends`.
- `AlertService.API/Services/IAlertService.cs` — add `GetTrendsAsync(int days, CancellationToken)` signature.
- `AlertService.API/Services/AlertManagementService.cs` — implement `GetTrendsAsync` using repository and UTC day range.
- `AlertService.Data/Interfaces/IAlertRepository.cs` — add `GetTrendsAsync(DateTime startUtc, DateTime endUtc, CancellationToken)` signature.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs` — implement `GetTrendsAsync` aggregation query.
- `AlertService.DTO/Responses/AlertTrendsResponse.cs` — new DTO to represent per-day buckets and severity counts (will reuse `AlertSeverityCountsResponse`).

Tests likely to be added/updated:
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` — add cases for valid days, default days, invalid days -> 400, and empty day buckets included.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` — add service-level aggregation logic tests.

Risk and Rollback:
- Low risk: additive endpoint and service/repository method. If issues occur, revert new interface/method additions and remove controller action to roll back.
