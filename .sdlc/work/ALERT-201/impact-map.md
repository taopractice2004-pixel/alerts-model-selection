# ALERT-201 Impact Map

## Primary Source Slice
- `AlertService.DTO/Requests/AlertQueryRequest.cs`: add query parameters and range validation
- `AlertService.API/Controllers/AlertsController.cs`: existing GET `/api/alerts` query-binding surface reused without code changes
- `AlertService.API/Services/AlertManagementService.cs`: pass-through for query filters
- `AlertService.Data/Interfaces/IAlertRepository.cs`: repository contract for alert list filtering
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`: inclusive created-date filtering implementation

## Focused Tests
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

## Out-of-Scope Edges
- UI or client changes
- Relative date presets
- Schema, migration, or SQL script changes
