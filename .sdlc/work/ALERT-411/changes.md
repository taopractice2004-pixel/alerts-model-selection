# Changes for ALERT-411 - Duplicate Alert Suppression Window

Files created/updated as part of implementation:

- AlertService.API/Configurations/AlertDeduplicationOptions.cs (new)
- AlertService.API/appsettings.json (updated: added AlertDeduplication default)
- AlertService.API/Controllers/AlertsController.cs (updated: return suppression header and 200 OK when duplicate suppressed)
- AlertService.API/Services/IAlertService.cs (updated: CreateAsync now returns CreateAlertResult)
- AlertService.API/Services/AlertManagementService.cs (updated: deduplication check and CreateAlertResult return)
- AlertService.Data/Interfaces/IAlertRepository.cs (updated: added FindActiveByTitleAndSeveritySinceAsync)
- AlertService.Data.SQL/Repositories/AlertRepository.cs (updated: implemented FindActiveByTitleAndSeveritySinceAsync)
- AlertService.DTO/Responses/CreateAlertResult.cs (new)
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs (updated: pass options and adapt to CreateAlertResult)
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs (updated: adapt to CreateAlertResult)

Validation:
- Ran focused unit tests: `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter Category!=Integration` — all tests passed.

Notes:
- Default suppression window is 15 minutes (configurable via `AlertDeduplication:SuppressionWindowMinutes`).
- Implementation keeps deduplication at service layer and adds a small repository helper for the query.
