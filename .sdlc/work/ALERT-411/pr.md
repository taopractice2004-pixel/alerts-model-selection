# ALERT-411: Add Duplicate Alert Suppression For Alert Creation

## PR Description
Implement duplicate alert suppression for POST /api/alerts so duplicate active alerts within a configurable window return the existing alert instead of creating a new record.
Keep normal creation behavior unchanged when no suppressible duplicate exists.

## Story / Requirement Summary
- Add configurable duplicate-alert suppression for POST /api/alerts using a 15-minute active-alert window by title and severity.
- Acceptance criteria covered: AC1, AC2, AC3, AC4, AC5.

## Implementation Summary
- Added suppression behavior in alert creation flow to detect existing active alerts by case-insensitive title, matching severity, and suppression time window.
- Updated API create response behavior to return 200 and X-Duplicate-Suppressed: true when suppression occurs, otherwise preserve 201 CreatedAtRoute.
- Added response model support for suppression signaling and made suppression window configuration-driven from appsettings.
- Extended repository duplicate lookup and corresponding unit tests across controller, service, and repository layers.

## Changed Files Summary
Diff source: git diff --name-only (read-only)

1. AlertService.API/Controllers/AlertsController.cs
2. AlertService.API/Services/AlertManagementService.cs
3. AlertService.Data/Interfaces/IAlertRepository.cs
4. AlertService.Data.SQL/Repositories/AlertRepository.cs
5. AlertService.DTO/Responses/AlertResponse.cs
6. AlertService.API/appsettings.json
7. AlertService.API.Tests/Controllers/AlertsControllerTests.cs
8. AlertService.API.Tests/Services/AlertManagementServiceTests.cs
9. AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

## Acceptance Criteria Traceability
- AC1 -> Implemented in AlertService.API/Services/AlertManagementService.cs and AlertService.Data.SQL/Repositories/AlertRepository.cs; validated by AlertService.API.Tests/Services/AlertManagementServiceTests.cs and AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs.
- AC2 -> Implemented in AlertService.API/Controllers/AlertsController.cs and AlertService.DTO/Responses/AlertResponse.cs; validated by AlertService.API.Tests/Controllers/AlertsControllerTests.cs.
- AC3 -> Implemented in AlertService.API/Controllers/AlertsController.cs and AlertService.API/Services/AlertManagementService.cs; validated by AlertService.API.Tests/Controllers/AlertsControllerTests.cs and AlertService.API.Tests/Services/AlertManagementServiceTests.cs.
- AC4 -> Implemented in AlertService.API/Services/AlertManagementService.cs and AlertService.Data.SQL/Repositories/AlertRepository.cs; validated by AlertService.API.Tests/Services/AlertManagementServiceTests.cs and AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs.
- AC5 -> Implemented in AlertService.API/Services/AlertManagementService.cs and AlertService.API/appsettings.json; validated by AlertService.API.Tests/Services/AlertManagementServiceTests.cs.

## Recorded Build / Test Results
- Build: dotnet build AlertService.sln - PASSED
- Unit tests:
  - dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj - PASSED (55/55)
  - dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj - PASSED (32/32)
- Coverage:
  - dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --collect:"XPlat Code Coverage" - PASSED (artifact generated)
  - dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --collect:"XPlat Code Coverage" - NOT_CONFIGURED (collector not found in current tooling)

## Configuration / Database / Migration Impact
- Configuration: AlertService.API/appsettings.json includes DuplicateWindowMinutes for alert suppression.
- Database / migrations: None.

## Known Risks / Limitations
- Tie-break behavior when multiple active duplicate matches exist is currently newest-by-created-date; business confirmation may still be needed.