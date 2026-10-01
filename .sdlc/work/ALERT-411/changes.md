# Changes - ALERT-411

## Summary
- Added configurable near-duplicate suppression to `POST /api/alerts`, returning the existing active alert with `200 OK` and `X-Duplicate-Suppressed: true` when a matching recent alert is found.
- Extended the service and repository create flow to look up active duplicates by trimmed title, severity, and configurable recent-window boundary using `TimeProvider`.
- Added focused controller, service, and repository tests for suppressed creates, newly created alerts, disabled suppression, and repository duplicate matching rules.

## Files Updated
- `AlertService.API\Controllers\AlertsController.cs`
- `AlertService.API\Services\AlertManagementService.cs`
- `AlertService.API\Services\IAlertService.cs`
- `AlertService.API\appsettings.json`
- `AlertService.API.Tests\Controllers\AlertsControllerTests.cs`
- `AlertService.API.Tests\Services\AlertManagementServiceTests.cs`
- `AlertService.Data\Interfaces\IAlertRepository.cs`
- `AlertService.Data.SQL\Repositories\AlertRepository.cs`
- `AlertService.Data.SQL.Tests\Repositories\AlertRepositoryTests.cs`
- `.sdlc/work/ALERT-411/changes.md`
- `.sdlc/work/ALERT-411/session.md`

## Validation
- `dotnet test AlertService.API.Tests\AlertService.API.Tests.csproj --no-restore`
- `dotnet test AlertService.Data.SQL.Tests\AlertService.Data.SQL.Tests.csproj --no-restore`
- `dotnet build AlertService.sln --no-restore`
