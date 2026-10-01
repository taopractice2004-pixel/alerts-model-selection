# Implementation Plan: ALERT-411

Authoritative file inventory, validation commands, and design decisions live in
`implementation-cache.json`. This plan sequences the work; it does not repeat those lists.

## Sequencing
1. **Configuration**
   - Add `"AlertSuppression": { "DuplicateWindowMinutes": 15 }` to
     `AlertService.API/appsettings.json`.
   - Add `AlertService.API/Options/AlertSuppressionOptions.cs` with `int DuplicateWindowMinutes`.
   - Register binding in `Program.cs`:
     `builder.Services.Configure<AlertSuppressionOptions>(builder.Configuration.GetSection("AlertSuppression"));`.
2. **Constants**
   - Add `AlertConstants.DuplicateSuppressedHeaderName = "X-Duplicate-Suppressed"`.
3. **Repository contract + implementation**
   - Extend `IAlertRepository` with
     `Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity, DateTime createdAfter, CancellationToken cancellationToken = default)`.
   - Implement in `AlertRepository`: case-insensitive title match (`.ToLower()` pattern already
     used for `search`), `IsActive == true`, `Severity == severity`,
     `CreatedDate >= createdAfter`, ordered by `CreatedDate` descending, `FirstOrDefaultAsync`.
4. **Service layer**
   - `IAlertService.CreateAsync` return type becomes `CreateAlertResult` (new
     `CreateAlertStatus { Created, DuplicateSuppressed }` enum + `CreateAlertResult(Status,
     Alert)` record), mirroring `AddAlertTagsResult`/`AddAlertTagsStatus`.
   - `AlertManagementService.CreateAsync`: before building/persisting the new entity, compute
     `cutoff = _timeProvider.GetUtcNow().UtcDateTime - TimeSpan.FromMinutes(options.Value.DuplicateWindowMinutes)`
     and call `FindActiveDuplicateAsync(request.Title, request.Severity, cutoff, ...)`. If a
     match exists, return `CreateAlertResult(DuplicateSuppressed, match.ToResponse())` without
     persisting. Otherwise proceed with the existing `ToEntity`/`AddAsync` flow and return
     `CreateAlertResult(Created, created.ToResponse())`.
   - Inject `IOptions<AlertSuppressionOptions>` into `AlertManagementService`'s constructor.
5. **Controller**
   - `Create` branches on `result.Status`: `Created` → existing `CreatedAtRoute(...)` (201);
     `DuplicateSuppressed` → set
     `Response.Headers[AlertConstants.DuplicateSuppressedHeaderName] = "true"` then `Ok(result.Alert)`
     (200).
6. **Tests**
   - Extend `AlertManagementServiceTests` for: new alert (Created), exact duplicate within window
     (DuplicateSuppressed, no repository `AddAsync` call), different severity (not suppressed),
     inactive prior match (not suppressed), match older than the window (not suppressed).
   - Extend `AlertsControllerTests` for: 201 path unchanged, 200 + `X-Duplicate-Suppressed: true`
     header path.
   - Extend `AlertRepositoryTests` for `FindActiveDuplicateAsync`: case-insensitive title match,
     severity filter, active-only filter, window boundary.
7. **Validation**
   - `dotnet build AlertService.sln`, `dotnet test`.

## Risks
- Changing `IAlertService.CreateAsync`'s return type is a breaking signature change; all call
  sites (controller, tests) must be updated together in the same change.
- Must not alter existing non-duplicate create behavior (timestamps, trimming, logging) for the
  `Created` path.
- Time-window comparison must use the same `TimeProvider` abstraction already used elsewhere in
  `AlertManagementService` to stay testable (no `DateTime.UtcNow` calls).
