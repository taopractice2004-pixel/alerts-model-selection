# Changes — ALERT-411

## Stage Summary
- Timestamp: 2026-09-30T00:00:00Z
- Command: /implement-story
- Files changed:
  - AlertService.Common/Constants/AlertConstants.cs
  - AlertService.Data/Interfaces/IAlertRepository.cs
  - AlertService.Data.SQL/Repositories/AlertRepository.cs
  - AlertService.API/Services/IAlertService.cs
  - AlertService.API/Services/CreateAlertResult.cs (new)
  - AlertService.API/Services/AlertManagementService.cs
  - AlertService.API/Controllers/AlertsController.cs
  - AlertService.API/appsettings.json
  - AlertService.API.Tests/Services/AlertManagementServiceTests.cs
  - AlertService.API.Tests/Controllers/AlertsControllerTests.cs
  - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- Description: `POST /api/alerts` now suppresses near-duplicate alert creation. Added
  `IAlertRepository.FindRecentActiveDuplicateAsync` (EF Core query: active + same severity +
  same title case-insensitive + created on/after a cutoff, most recent match). In
  `AlertManagementService.CreateAsync`, the suppression window (minutes, default 15) is read via
  `IConfiguration.GetValue` from a new `Alerts:DuplicateSuppressionWindowMinutes` config key
  (matches the existing direct-`IConfiguration` convention in `Program.cs`); the cutoff is
  computed from the injected `TimeProvider`. When a match is found, the existing alert is
  returned instead of persisting a new row. `AlertsController.Create` sets response header
  `X-Duplicate-Suppressed: true` and returns `200 OK` on suppression; a genuine new alert still
  returns `201 Created` with no header.

## Unresolved-Question Resolutions
- Duplicate-signal shape from service to controller: added a small `CreateAlertResult(AlertResponse
  Alert, bool IsDuplicate)` record in `AlertService.API/Services`, replacing
  `IAlertService.CreateAsync`'s previous `Task<AlertResponse>` return type. No other callers existed.
- Config key: `Alerts:DuplicateSuppressionWindowMinutes`, default `15` (also set explicitly in
  `appsettings.json`), stored as a named constant in `AlertConstants` alongside the default value.

## Validation Results
- `dotnet build AlertService.sln` — succeeded (all 8 projects).
- `dotnet test AlertService.sln --no-build` — 84/84 passed.

## Coverage Results
- NOT_CONFIGURED (no coverage tooling in repo; behavior coverage added via new/updated unit tests:
  suppress within window same title/severity active, different severity not suppressed, window
  expired not suppressed, prior alert inactive not suppressed, case-insensitive title match
  suppressed, configurable window respected, controller 200+header vs 201 paths).

## Reproduction Results
- NOT_RUN (no reproduction commands defined for this story).

## Standards Applied
- coding-standards.md, backend-dotnet-standards.md, api-rest-standards.md,
  service-architecture-standards.md, database-standards.md.

## Deferred Items
- None.
