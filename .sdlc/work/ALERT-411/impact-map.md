Impact Map (compact)

- `AlertService.API/Controllers/AlertsController.cs`: adjust `Create` to surface `X-Duplicate-Suppressed` header and return 200 when suppression applies.
- `AlertService.API/Services/AlertManagementService.cs`: add suppression logic using configurable window; return indicator for suppressed vs created.
- `AlertService.Data/Interfaces/IAlertRepository.cs`: (optional) add `FindActiveByTitleAndSeveritySinceAsync` to support precise query.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`: (optional) implement new repository method using EF query on `Alerts` table with `IsActive`, `Severity`, and normalized `Title` equality and `CreatedDate` filter.
- `AlertService.API/appsettings.json`: add `AlertDeduplication:SuppressionWindowMinutes` setting.
- `AlertService.API.Tests/*`: update/extend tests for suppression behavior (service and controller tests).
