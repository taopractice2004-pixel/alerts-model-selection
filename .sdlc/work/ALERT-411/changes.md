# Changes - ALERT-411

## Implemented
- Added configurable duplicate suppression on `POST /api/alerts` using `AlertSuppression:DuplicateWindowMinutes` from `appsettings.json`.
- Added a service-level create outcome contract so the controller can return `200 OK` plus `X-Duplicate-Suppressed: true` for suppressed duplicates and `201 Created` for new alerts.
- Added a repository duplicate lookup filtered by case-insensitive title, exact severity, active status, and created-since cutoff.
- Added focused controller, service, and repository tests for duplicate-suppressed vs newly created alert behavior.

## Validation
- `DOTNET_ROLL_FORWARD=Major dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj`
  Result: story-specific create tests passed; one unrelated pre-existing controller validation test still fails (`AlertQueryRequest_WithInvalidValues_FailsValidation`).
- `DOTNET_ROLL_FORWARD=Major dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj`
  Result: passed.
- `dotnet build AlertService.sln`
  Result: passed.

## Notes
- Seeded `AlertSuppression:DuplicateWindowMinutes` to `5` in `appsettings.json` so the setting is explicit and configurable.