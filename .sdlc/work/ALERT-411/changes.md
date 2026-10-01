# Changes — ALERT-411

## Stage Summary
- Timestamp: 2026-10-01T17:07:38+05:30
- Command: /implement-story
- Files changed:
  - Source (created):
    - `AlertService.API/Configuration/AlertSuppressionOptions.cs` — strongly-typed options (`WindowMinutes = 15` default, `SectionName = "AlertSuppression"`).
    - `AlertService.API/Services/AlertCreateResult.cs` — `public sealed record AlertCreateResult(AlertResponse Alert, bool WasSuppressed)`.
  - Source (updated):
    - `AlertService.API/appsettings.json` — added `"AlertSuppression": { "WindowMinutes": 15 }`.
    - `AlertService.API/Program.cs` — registered options via `Configure<AlertSuppressionOptions>(GetSection(SectionName))`.
    - `AlertService.Data/Interfaces/IAlertRepository.cs` — added `FindActiveDuplicateAsync(...)`.
    - `AlertService.Data.SQL/Repositories/AlertRepository.cs` — implemented EF-translatable duplicate lookup (active + same severity + case-insensitive trimmed title + `CreatedDate >= cutoff`, most-recent first).
    - `AlertService.API/Services/IAlertService.cs` — `CreateAsync` returns `Task<AlertCreateResult>`.
    - `AlertService.API/Services/AlertManagementService.cs` — injected `IOptions<AlertSuppressionOptions>`; `CreateAsync` computes UTC window via `TimeProvider`, suppresses on duplicate (returns existing + flag + log), else creates and returns not-suppressed. No EF Core reference.
    - `AlertService.API/Controllers/AlertsController.cs` — `Create` returns `Ok(result.Alert)` + header `X-Duplicate-Suppressed: true` when suppressed, else `CreatedAtRoute(...)`; documents both 200 and 201.
  - Tests (compile-correctness only, existing tests kept green):
    - `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` — mock returns `new AlertCreateResult(...)`.
    - `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` — passes `Options.Create(new AlertSuppressionOptions())`, unwraps assertions to `result.Alert`.
    - `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` — no change required (compiles as-is).
- Description: Implemented configurable near-duplicate alert suppression on `POST /api/alerts`. An active alert with the same trimmed, case-insensitive Title and same Severity created within the configured window (default 15 min) returns 200 OK with the existing alert and header `X-Duplicate-Suppressed: true` (no new row); otherwise a new alert is created and 201 is returned. EF Core remains confined to the repository; window math uses the injected UTC `TimeProvider`.

## Validation Results
- `dotnet build` → Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test --no-build` → PASS. `AlertService.Data.SQL.Tests`: 25 passed / 0 failed. `AlertService.API.Tests`: 37 passed / 0 failed.

## Coverage Results
- NOT_CONFIGURED

## Reproduction Results
- NOT_RUN

## Standards Applied
- coding, backend-dotnet, api-rest, service-architecture, database

## Deferred Items
- New feature-specific unit tests for suppression behavior (controller 200+header vs 201, service suppressed-vs-created, repository duplicate query) are deferred to the `/unit-testing` stage; only existing tests were kept compiling here.
