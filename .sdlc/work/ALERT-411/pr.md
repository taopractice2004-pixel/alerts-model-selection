# ALERT-411 — Suppress near-duplicate alerts within a configurable window

## Description
Add a configurable duplicate-suppression window to the alert create path so a near-duplicate
`POST /api/alerts` returns the existing alert (200 OK) instead of inserting a new row, while
genuinely new alerts still return 201 Created.

## Story / Requirement Summary
Suppress near-duplicate alerts on `POST /api/alerts` within a configurable time window, returning
the existing alert instead of creating a new row.

Acceptance criteria:
- **AC1** — Active alert with same Title (case-insensitive) and same Severity created within the
  last N minutes is not duplicated (no new row).
- **AC2** — Suppressed request returns 200 OK with the existing alert and header
  `X-Duplicate-Suppressed: true`.
- **AC3** — Genuinely new alert returns 201 Created with no suppression header set to true.
- **AC4** — Suppression window (minutes) is read from `appsettings.json`, not hardcoded.
- **AC5** — Same Title but different Severity is not suppressed (new alert created).
- **AC6** — A prior matching alert that is inactive does not cause suppression (new alert created).

## Implementation Summary
- Added `AlertSuppressionOptions` bound from `AlertSuppression:WindowMinutes` (default 15; a value
  ≤ 0 disables suppression) and registered it in `Program.cs`; added the config section to
  `appsettings.json`.
- Added `CreateAlertResult` / `CreateAlertStatus` (Created vs Suppressed) following the existing
  result-object pattern instead of throwing.
- Added `IAlertRepository.FindRecentDuplicateAsync` + SQL implementation: active + same Severity +
  case-insensitive/trimmed Title + `CreatedDate >= cutoff` (inclusive), most-recent first,
  `AsNoTracking`.
- `AlertManagementService.CreateAsync` computes the cutoff from the injected `TimeProvider` and the
  configured window and returns Created vs Suppressed (no `HttpContext` dependency); when the
  window ≤ 0 it skips the lookup.
- `AlertsController.Create` maps Suppressed → 200 OK + `X-Duplicate-Suppressed: true`, Created →
  201 `CreatedAtRoute`.

## Changed Files (source: `git status --porcelain` / `git diff --stat`)
Production:
- `AlertService.API/Configuration/AlertSuppressionOptions.cs` (new)
- `AlertService.DTO/Responses/CreateAlertResult.cs` (new)
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Program.cs`
- `AlertService.API/appsettings.json`

Tests:
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

Diff stat: 10 tracked files changed, 265 insertions(+), 14 deletions(-); plus new untracked files
`AlertService.API/Configuration/` and `AlertService.DTO/Responses/CreateAlertResult.cs`.

## Acceptance Criteria → Implementation / Validation Traceability
| AC | Implemented in | Proven by test |
|---|---|---|
| AC1 | `AlertManagementService.CreateAsync` + `AlertRepository.FindRecentDuplicateAsync` | `AlertManagementServiceTests.CreateAsync_WhenRecentActiveDuplicateExists_SuppressesAndReturnsExisting`; `AlertRepositoryTests.FindRecentDuplicateAsync_WhenActiveMatchWithinWindow_ReturnsAlert` |
| AC2 | `AlertsController.Create` (Suppressed → 200 + header) | `AlertsControllerTests.Create_WhenSuppressed_ReturnsOk_WithExistingAlert_AndDuplicateHeader` |
| AC3 | `AlertManagementService.CreateAsync` + `AlertsController.Create` (Created → 201) | `AlertManagementServiceTests.CreateAsync_WhenNoDuplicate_CreatesAndReturnsCreatedStatus`; `AlertsControllerTests.Create_WhenCreated_ReturnsCreatedAtRoute_WithLocationId` |
| AC4 | `AlertSuppressionOptions` + `Program.cs` + `appsettings.json`; cutoff from `TimeProvider` | `AlertManagementServiceTests.CreateAsync_UsesConfiguredWindow_ToComputeCutoffFromTimeProvider`; `CreateAsync_WhenWindowDisabled_SkipsDuplicateLookup_AndCreates` |
| AC5 | `FindRecentDuplicateAsync` severity match | `AlertRepositoryTests.FindRecentDuplicateAsync_WhenSeverityDiffers_ReturnsNull`; `AlertManagementServiceTests.CreateAsync_ForwardsRequestTitleAndSeverity_ToDuplicateLookup` |
| AC6 | `FindRecentDuplicateAsync` active-only filter | `AlertRepositoryTests.FindRecentDuplicateAsync_WhenMatchIsInactive_ReturnsNull` |

## Test / Build Results (already recorded; not re-run)
- Build: `dotnet test` for both test projects → SUCCEEDED.
- `AlertService.API.Tests` → 66/66 passed.
- `AlertService.Data.SQL.Tests` → 41/41 passed.
- Acceptance criteria: AC1–AC6 all MET.
- Coverage: NOT_CONFIGURED (no coverage tooling in the repository).

## Configuration / Database / Migration Impacts
- Configuration: new `AlertSuppression:WindowMinutes` section in `appsettings.json` (default 15;
  ≤ 0 disables suppression).
- Database / migrations: None — suppression is a read-before-insert check; no schema change.

## Known Risks / Limitations
- Case-insensitive Title matching relies on the existing provider-translatable query style
  (`AsNoTracking`, `ToLower()`); verify behavior against the target SQL collation.
- `IAlertService.CreateAsync` return shape changed (Created vs Suppressed result object);
  callers and tests were updated accordingly.
