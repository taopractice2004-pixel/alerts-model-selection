FILES UPDATED:
- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data/Interfaces/DailyAlertTrend.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.DTO/Requests/AlertTrendsRequest.cs
- AlertService.DTO/Responses/AlertTrendsResponse.cs
- AlertService.DTO/Responses/DailyAlertTrendResponse.cs

SUMMARY:
Implemented the Alert Trends endpoint `GET /api/alerts/trends?days=N`.
Added DTOs and request model, repository aggregation, service mapping, and controller action.
