# Changes: ALERT-411 — Near-duplicate alert suppression

## Files touched/created

- **AlertService.API/appsettings.json** — Added `AlertSuppression:DuplicateWindowMinutes` section
  with value `15` so the suppression window is configurable, not hardcoded.
- **AlertService.API/Options/AlertSuppressionOptions.cs** (new) — New options class with
  `int DuplicateWindowMinutes` (default 15), bound from the `AlertSuppression` config section.
- **AlertService.API/Program.cs** — Registered
  `builder.Services.Configure<AlertSuppressionOptions>(builder.Configuration.GetSection("AlertSuppression"))`
  alongside the existing service registrations.
- **AlertService.Common/Constants/AlertConstants.cs** — Added
  `DuplicateSuppressedHeaderName = "X-Duplicate-Suppressed"` constant.
- **AlertService.API/Services/IAlertService.cs** — Added `CreateAlertStatus { Created, DuplicateSuppressed }`
  enum and `CreateAlertResult(CreateAlertStatus Status, AlertResponse Alert)` record, mirroring the
  existing `AddAlertTagsResult`/`AddAlertTagsStatus` pattern. Changed `CreateAsync` to return
  `CreateAlertResult` instead of a bare `AlertResponse`.
- **AlertService.API/Services/AlertManagementService.cs** — Injected `IOptions<AlertSuppressionOptions>`.
  `CreateAsync` now computes `cutoff = _timeProvider.GetUtcNow().UtcDateTime - TimeSpan.FromMinutes(options.Value.DuplicateWindowMinutes)`,
  calls the new repository duplicate-lookup method, and returns `CreateAlertResult(DuplicateSuppressed, existing)`
  when a match is found or `CreateAlertResult(Created, newAlert)` otherwise. Logs the suppression at
  Information level (consistent with existing logging style).
- **AlertService.Data/Interfaces/IAlertRepository.cs** — Added
  `Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfter, CancellationToken cancellationToken = default)`.
- **AlertService.Data.SQL/Repositories/AlertRepository.cs** — Implemented `FindActiveDuplicateAsync`
  using the existing `.ToLower()` case-insensitive title comparison pattern, filtered by
  `IsActive == true && Severity == severity && CreatedDate >= createdAfter`, ordered by
  `CreatedDate` descending, returning the first match (or `null`).
- **AlertService.API/Controllers/AlertsController.cs** — `Create` now branches on
  `CreateAlertStatus`: `Created` → `CreatedAtRoute(nameof(GetById), new { id = result.Alert.Id }, result.Alert)`
  (201); `DuplicateSuppressed` → sets `Response.Headers[AlertConstants.DuplicateSuppressedHeaderName] = "true"`
  and returns `Ok(result.Alert)` (200). Added a `[ProducesResponseType(... Status200OK)]` annotation
  for the new duplicate-suppressed response shape.

## Test files updated/added

- **AlertService.API.Tests/Services/AlertManagementServiceTests.cs** — Updated constructor to inject
  `IOptions<AlertSuppressionOptions>` and a default "no duplicate" repository stub. Updated the
  existing `CreateAsync` happy-path test to read `result.Status`/`result.Alert`. Added:
  `CreateAsync_WhenNoDuplicateExists_ReturnsCreatedStatus_AndChecksRepositoryWithConfiguredCutoff`
  (verifies the repository duplicate-lookup is called with the cutoff derived from the injected
  options and fixed time provider), `CreateAsync_WhenActiveDuplicateWithinWindowExists_SuppressesCreation_AndReturnsExistingAlert`
  (verifies `DuplicateSuppressed` status, existing alert returned, `AddAsync` never called),
  `CreateAsync_WhenRepositoryReportsNoDuplicateForDifferentSeverity_CreatesNewAlert`, and
  `CreateAsync_WhenRepositoryReportsNoDuplicateForInactiveMatch_CreatesNewAlert`.
- **AlertService.API.Tests/Controllers/AlertsControllerTests.cs** — Added `ControllerContext` with a
  `DefaultHttpContext` so `Response.Headers` is usable in tests. Updated
  `Create_ReturnsCreatedAtRoute_WithLocationId` to stub a `CreateAlertResult(Created, ...)`. Added
  `Create_WhenDuplicateSuppressed_ReturnsOk_WithSuppressionHeader` verifying 200 OK and the
  `X-Duplicate-Suppressed: true` response header.
- **AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs** — Added
  `FindActiveDuplicateAsync_WithMatchingTitleCaseInsensitive_SeverityAndWithinWindow_ReturnsAlert`,
  `FindActiveDuplicateAsync_WithDifferentSeverity_ReturnsNull`,
  `FindActiveDuplicateAsync_WithInactiveMatch_ReturnsNull`,
  `FindActiveDuplicateAsync_WithMatchOutsideWindow_ReturnsNull`, and
  `FindActiveDuplicateAsync_WithMultipleMatches_ReturnsMostRecent`.

## Validation

- `dotnet build AlertService.sln` — **PASSED** (0 errors).
- `dotnet test` — **PASSED** (AlertService.Data.SQL.Tests: 30/30 passed; AlertService.API.Tests: 42/42
  passed; 0 failed overall).

No deviations from the cached scope in `implementation-cache.json` / `impact-map.md` were required;
all touched files match the exact list specified there.
