Implementation Plan — ALERT-412

Goal: Add `GET /api/alerts/trends?days=N` returning one UTC-calendar-day bucket per day (oldest-first) for the last N days, each bucket containing total count and counts per severity.

Tasks:
1. Add DTOs: `AlertTrendsResponse` and `DailyAlertTrendResponse` (each day uses `AlertSeverityCountsResponse`).
2. Add repository method `IAlertRepository.GetTrendsAsync(DateTime startUtc, DateTime endUtc, CancellationToken)` and implement aggregation in `AlertRepository`.
3. Add service method `IAlertService.GetTrendsAsync(int days, CancellationToken)` and implement in `AlertManagementService` to construct day range and call repository, ensuring UTC calendar days and ordering oldest-first.
4. Add controller action `GET api/alerts/trends?days=N` in `AlertsController` with `[FromQuery, Range(1,90)] int days = 7` (relies on `[ApiController]` for `ValidationProblemDetails`).
5. Add unit tests: controller and service tests to validate behavior for boundary `days`, invalid `days` -> 400, and zero-count days/severities are present.
6. Run `dotnet test` and iterate on any failing tests.

Notes and rationale:
- Use `AlertSeverityCountsResponse` ordering (Low, Medium, High, Critical) to remain consistent with existing summary endpoint.
- Repository should aggregate by `CreatedDate.Date` (UTC) and then the service must ensure the day range uses UTC dates (DateTime.UtcNow.Date - (days-1)).
- For invalid `days`, rely on model validation provided by `[ApiController]` producing `ValidationProblemDetails`.
