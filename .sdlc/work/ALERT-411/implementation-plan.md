# Implementation Plan — ALERT-411

## Change Strategy
- Add a `DuplicateSuppressionOptions` class (`WindowMinutes`, default 15) bound from the
  `Alerts:DuplicateSuppression` section in `appsettings.json` and registered in `Program.cs`.
- Add a repository method on `IAlertRepository`/`AlertRepository` that finds the most recent
  active alert matching a title (case-insensitive) and severity created at/after a threshold
  timestamp. Server-side filtering only; no `SELECT *`.
- In `AlertManagementService.CreateAsync`, compute the window threshold from `TimeProvider` and
  the configured window, query for an active duplicate, and either return the existing alert
  flagged as suppressed or create the new alert as today. Return a suppression-aware result
  instead of a bare `AlertResponse`.
- Update `IAlertService.CreateAsync` to return the suppression-aware result and adjust
  `AlertsController.Create` to emit `201 Created` for a real create or `200 OK` plus the
  `X-Duplicate-Suppressed: true` header when suppressed.

## Validation Strategy
- `dotnet build AlertService.sln` then `dotnet test` — the existing controller, service, and
  repository test projects cover every changed layer, so the full solution test run is the
  narrowest sufficient gate. Add focused unit tests for suppression, genuine create,
  severity mismatch, inactive prior alert, and outside-window create.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Slice spans controller → service → data → config because suppression is a create-path
  behavior with a configurable, persisted lookup; it stays confined to the create flow.
