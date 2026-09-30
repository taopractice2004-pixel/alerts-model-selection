# ALERT-411 Changes

## Summary
- Added duplicate alert suppression for `POST /api/alerts` using configurable suppression window minutes.
- Duplicate match criteria now enforce: same title (case-insensitive), same severity, active alert only, and created within suppression window.
- Suppressed duplicates now return `200 OK` with header `X-Duplicate-Suppressed: true` and the existing alert payload.
- New alert creation path still returns `201 Created`.

## Source Changes
- API layer
  - `AlertService.API/Controllers/AlertsController.cs`
  - `AlertService.API/Program.cs`
  - `AlertService.API/Services/IAlertService.cs`
  - `AlertService.API/Services/AlertManagementService.cs`
  - `AlertService.API/appsettings.json`

## Test Changes
- `AlertService.API.Tests/IntegrationTests/TestCasesCsvTests.cs` (existing ALERT-411 expectations now satisfied by implementation)
- `AlertService.API.Tests/TestInfrastructure/AlertsApiFactory.cs` (suppression config key aligned)
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` (create-path suppression/non-suppression coverage)
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` (create response branching and suppression header coverage)

## Validation
- Passed
  - `dotnet build AlertService.sln`
  - `dotnet test AlertService.API.Tests --filter "FullyQualifiedName~TC_411_|FullyQualifiedName~AlertManagementServiceTests"`
