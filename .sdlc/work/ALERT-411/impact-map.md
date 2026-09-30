# Impact Map - ALERT-411

**Direct path:** `POST /api/alerts` -> `AlertsController.Create` -> `IAlertService.CreateAsync` -> `AlertManagementService` -> `IAlertRepository` -> SQL/EF repository.

**Contract impact:** POST has two success outcomes: existing response with suppression header (`200`) or new response (`201`). Other endpoints remain unchanged.

**Data impact:** Read-only duplicate lookup against existing alert columns; no schema migration expected. Database query performance should be reviewed against title, severity, active state, and created date.

**Out of scope:** alert updates/deactivation semantics, other endpoints, frontend changes, and unrelated test failures.

See `implementation-cache.json` for exact inventory and validation scope.
