# Impact Map — ALERT-411

## Primary Slice
- `AlertService.API/Controllers/AlertsController.cs` (`Create`: 200 + `X-Duplicate-Suppressed`
  header vs 201 path)
- `AlertService.API/Services/AlertManagementService.cs` (`CreateAsync`: duplicate lookup, cutoff
  computation, config read)
- `AlertService.API/Services/IAlertService.cs` (`CreateAsync` contract change to signal
  suppression)

## Adjacent Dependencies
- `AlertService.Data/Interfaces/IAlertRepository.cs` (new recent-active-duplicate lookup method)
- `AlertService.Data.SQL/Repositories/AlertRepository.cs` (EF Core implementation of the lookup)
- `AlertService.API/appsettings.json` (new `Alerts` suppression-window setting)

## Out Of Scope
- `AlertService.DTO/Responses/AlertResponse.cs` — unchanged; the suppressed response reuses the
  existing alert body, no new response fields needed.
- `AlertService.Data.SQL/Migrations/*` — no schema change; suppression only adds a read query.
- Tag-related files from ALERT-410 — unrelated to this story.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
