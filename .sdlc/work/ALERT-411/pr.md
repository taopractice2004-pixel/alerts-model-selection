# ALERT-411: Suppress duplicate alert creates within the configured window

## PR Description
Add configurable duplicate-alert suppression to the alert create flow so near-duplicate active alerts return the existing alert instead of creating a second row, while preserving normal create behavior for genuinely new alerts.

## Story / Requirement Summary
- AC1: `POST /api/alerts` returns `200 OK` with the existing alert and `X-Duplicate-Suppressed: true` when an active same-title, same-severity alert exists inside the configured suppression window.
- AC2: `POST /api/alerts` returns `201 Created` and persists a new alert when no active matching alert exists inside the configured suppression window.
- AC3: The duplicate suppression window is configuration-driven through `appsettings.json`.
- AC4: Duplicate suppression does not apply across different severities or inactive alerts.

## Implementation Summary
- Added duplicate lookup support at the repository seam and used it in the alert create flow to decide whether to reuse an existing active alert or persist a new one.
- Extended the create service result so the controller can distinguish suppressed duplicates from genuine creates, returning `200 OK` plus `X-Duplicate-Suppressed: true` for duplicates and preserving `201 Created` for new alerts.
- Added scoped controller, service, and repository tests covering the duplicate response path, the normal create path, configuration-driven window behavior, and the inactive/different-severity escape conditions.

## Changed-Files Summary
- Source: read-only `git status --porcelain -- AlertService.API AlertService.Data.SQL AlertService.API.Tests AlertService.Data.SQL.Tests .sdlc/work/ALERT-411`
- Production: `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/IAlertService.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.API/appsettings.json`
- Tests: `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`
- Workflow artifacts: `.sdlc/work/ALERT-411/work.json`, `.sdlc/work/ALERT-411/log.md`, `.sdlc/work/ALERT-411/pr.md`

## Acceptance Criteria Traceability
| AC | Implementation | Validation |
|---|---|---|
| AC1 | `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |
| AC2 | `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` |
| AC3 | `AlertService.API/appsettings.json`, `AlertService.API/Services/AlertManagementService.cs` | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` |
| AC4 | `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs` | `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |

## Test / Build Results Already Recorded
- Build: `dotnet build AlertService.API/AlertService.API.csproj` - PASSED
- Unit tests: `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"` - PASSED (55/55)
- Unit tests: `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"` - PASSED (33/33)
- Coverage: `NOT_CONFIGURED`

## Configuration / Database / Migration Impacts
- Configuration: adds or uses `Alerts:DuplicateSuppressionWindowMinutes` in `AlertService.API/appsettings.json`.
- Database / migrations: None.

## Known Risks / Limitations
- Duplicate suppression is intentionally limited to case-insensitive title matching plus same severity, active status, and the configured time window.
- Alert list/detail/query behavior and update/deactivate flows remain out of scope.