# Impact Map — ALERT-411

## Primary Slice
- Duplicate suppression on the create path: `AlertsController.Create`,
  `IAlertService`/`AlertManagementService.CreateAsync`, `IAlertRepository`/`AlertRepository`
  duplicate lookup, a new `DuplicateSuppressionOptions`, and the `appsettings.json` window
  setting.

## Adjacent Dependencies
- `AlertService.Data/Interfaces/IAlertRepository.cs` (new duplicate-lookup contract consumed by the service).
- `AlertService.API/Program.cs` (bind + register the suppression options).

## Out Of Scope
- No changes to update, deactivate, delete, summary, list/filter, or tag endpoints.
- No schema/migration change (matching uses existing `Title`, `Severity`, `CreatedDate`, `IsActive`).
- No new frontend/UI (solution is backend-only); no auth/authorization changes.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
