Story: ALERT-412 — Alert Volume Trend Endpoint

As a dashboard consumer, I want daily alert-creation counts broken down by severity so I can chart trends.

Scope anchors (smallest useful):
- API route: `GET /api/alerts/trends?days=N` (default 7, min 1, max 90).
- Controller: `AlertService.API/Controllers/AlertsController.cs` — new action.
- Service: `IAlertService.GetTrendsAsync` / `AlertManagementService.GetTrendsAsync` — business logic.
- Persistence: `IAlertRepository.GetTrendsAsync` / `AlertRepository.GetTrendsAsync` — aggregation query.
- DTOs: new response type providing per-day total and `AlertSeverityCountsResponse` per day.

Classification: SIMPLE — deterministic implementation path discovered (controller + service + repository + DTOs).

Selected standards (short list):
- standards/api-rest-standards.md  (API shape, validation responses)
- standards/backend-dotnet-standards.md  (time handling: use UTC dates)
- standards/coding-standards.md  (naming, DTO layering)
