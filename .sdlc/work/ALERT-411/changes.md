# Changes — ALERT-411

## Implemented
- Added duplicate-suppression flow for `POST /api/alerts`:
  - `AlertManagementService.CreateAsync` now checks for an active alert with same title (case-insensitive) and severity inside a configured time window before inserting.
  - Added service result contract (`AlertCreateResult`) so the API can distinguish suppressed duplicate vs new create.
- Extended API create behavior:
  - Returns `200 OK` with existing alert when suppressed and sets header `X-Duplicate-Suppressed: true`.
  - Preserves existing `201 Created` + `CreatedAtRoute` response for newly created alerts.
- Extended repository contract/implementation:
  - Added `GetLatestActiveDuplicateAsync(title, severity, createdFromUtc)` to fetch the newest eligible duplicate.
- Added configurable suppression window:
  - Added `AlertDuplicateSuppression:WindowMinutes` in `appsettings.json`.
  - Wired options binding/validation in `Program.cs` with startup validation (`> 0`).

## Tests Updated
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
  - Added create-duplicate suppression response/header test.
  - Updated create test to use `AlertCreateResult`.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
  - Updated create-path expectations for `AlertCreateResult`.
  - Added suppression-path unit test ensuring duplicate suppression skips insert.
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`
  - Added duplicate query tests for case-insensitive title matching, active-only enforcement, severity match, and window cutoff behavior.

## Validation Run
- `dotnet build AlertService.sln`
- `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"`
- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"`
