# PR Draft — ALERT-411

## Title
ALERT-411: Suppress duplicate alerts within a configurable window

## Description
`POST /api/alerts` now returns the existing active alert (200 + `X-Duplicate-Suppressed: true`) instead of inserting a new row when an active alert with the same Title (case-insensitive) and Severity was created within a configurable window (default 15 minutes).

## Story / Requirement Summary
Duplicate Alert Suppression Window. See `work.json` → `summary` / `acceptance_criteria` (AC1–AC7).

## Implementation Summary
- New `DuplicateSuppressionOptions` bound from `AlertSuppression:DuplicateWindowMinutes` (default 15; 0 disables; negatives rejected at startup).
- New `CreateAlertResult` (alert + `DuplicateSuppressed`); `IAlertService.CreateAsync` now returns it.
- `IAlertRepository.FindRecentActiveDuplicateAsync` / `AlertRepository`: active, same severity, case-insensitive title, `CreatedDate >= now - window`, most recent match (parameterized EF query).
- `AlertManagementService.CreateAsync` checks for a duplicate (using `TimeProvider`) before insert; validation still runs first.
- `AlertsController.Create` returns 200 + header on suppression, otherwise 201 `CreatedAtRoute`.

## Changed Files (source: `git status` / `git diff --stat`, read-only; 14 files)
Production:
- `AlertService.API/Configuration/DuplicateSuppressionOptions.cs` (new)
- `AlertService.API/Services/CreateAlertResult.cs` (new)
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Program.cs`
- `AlertService.API/appsettings.json`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`

Tests:
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/HealthChecksTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

Pipeline artifacts: `.sdlc/work/ALERT-411/`

## Acceptance Criteria Traceability
| AC | Implementation | Validation |
|---|---|---|
| AC1 no insert on duplicate | `AlertManagementService.CreateAsync`, `AlertRepository.FindRecentActiveDuplicateAsync` | `CreateAsync_WhenActiveDuplicateInWindow_ReturnsExisting_AndDoesNotInsert`; repository `FindRecentActiveDuplicateAsync_*` (case-insensitive match) |
| AC2 200 + header + existing body | `AlertsController.Create` | `Create_WhenDuplicateSuppressed_ReturnsOkWithExistingAlert_AndHeader` |
| AC3 201 when no match | `AlertsController.Create`, service insert path | `CreateAsync_WhenNoDuplicate_InsertsAndIsNotSuppressed`; `Create_ReturnsCreatedAtRoute_WithLocationId` |
| AC4 window from config, default 15 | `DuplicateSuppressionOptions`, `Program.cs`, `appsettings.json` | `HealthChecksTests.DuplicateWindow_IsBoundFromAppSettings_DefaultingTo15`; `CreateAsync_UsesConfiguredWindowForLookup`; `CreateAsync_WhenWindowIsZero_SkipsLookup_AndInserts` |
| AC5 different severity not suppressed | repository lookup severity filter | repository `FindRecentActiveDuplicateAsync_*` (different severity) |
| AC6 inactive not suppressed | repository lookup `IsActive` filter | repository `FindRecentActiveDuplicateAsync_*` (inactive alert) |
| AC7 older than window not suppressed | repository lookup `CreatedDate` filter | repository `FindRecentActiveDuplicateAsync_*` (older than window, inclusive boundary) |

## Test / Build Results (already recorded, not re-run)
- Build: `dotnet build AlertService.API/AlertService.API.csproj` succeeded.
- `dotnet test AlertService.API.Tests`: 83 passed, 0 failed.
- `dotnet test AlertService.Data.SQL.Tests`: 53 passed, 0 failed.
- Coverage: changed API classes (`AlertManagementService`, `AlertsController`, `CreateAlertResult`, `DuplicateSuppressionOptions`) 100% line/branch; assembly-wide 48%.
- Test → fix loop: 0/3 — `TESTS_PASSED`; no open bugs.

## Configuration / Database / Migration Impacts
- Configuration: new `AlertSuppression:DuplicateWindowMinutes` in `appsettings.json` (default 15; 0 disables; negative fails startup).
- Database: no schema change or migration.
- API: `POST /api/alerts` can now return 200 with `X-Duplicate-Suppressed: true` (no `Location` header). `IAlertService.CreateAsync` return type changed to `CreateAlertResult`.

## Known Risks / Limitations
- Concurrent identical POSTs can both insert (no lock/unique constraint); suppression is best-effort.
- Assumptions (from analysis): inclusive window boundary, most recent match returned, 0 disables suppression, same `AlertResponse` body on suppression.
- `AlertService.API.http` and `README.md` not yet updated for the new 200 behavior and configuration key.
