# Impact Map — ALERT-411

## Primary Slice
- Duplicate-alert suppression on `POST /api/alerts`: configurable window, repository duplicate
  lookup, service result carrying suppressed-vs-created, controller 200+`X-Duplicate-Suppressed`
  header vs 201.

## Impacted Files by Layer
- **Config:** `AlertService.API/appsettings.json` (new `AlertSuppression` section),
  `AlertService.API/Configuration/AlertSuppressionOptions.cs` (new),
  `AlertService.API/Program.cs` (bind/register options).
- **Data:** `AlertService.Data/Interfaces/IAlertRepository.cs` (new lookup method),
  `AlertService.Data.SQL/Repositories/AlertRepository.cs` (EF query).
- **API service:** `AlertService.API/Services/IAlertService.cs`,
  `AlertService.API/Services/AlertManagementService.cs`,
  `AlertService.API/Services/AlertCreateResult.cs` (new).
- **API controller:** `AlertService.API/Controllers/AlertsController.cs`.

## Adjacent Dependencies
- Options binding wired in `Program.cs`; service consumes bound options + injected `TimeProvider`.
- Repository EF query reuses the existing case-insensitive `.ToLower()` translatable pattern.

## Out Of Scope
- **No DB schema / migration / SQL mirror change** — no new columns are introduced.
- GET/PUT and other endpoints, paging/sorting/filter semantics, and `GetSummaryAsync` unchanged.
- No new third-party dependencies.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
