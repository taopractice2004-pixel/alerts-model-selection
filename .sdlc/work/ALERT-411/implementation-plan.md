# Implementation Plan — ALERT-411

## Change Strategy
- Add a configurable suppression-window setting (minutes, default `15`) under a new `Alerts`
  section in `appsettings.json`; read it in `AlertManagementService` via injected `IConfiguration`
  (matches the existing direct-`GetValue` convention already used in `Program.cs`), not a new
  `IOptions<T>` type.
- Add an `IAlertRepository` lookup method that finds the most recent **active** alert matching
  `Title` (case-insensitive) and `Severity`, created on/after a cutoff timestamp; implement it in
  `AlertRepository` as a single EF Core query (`IsActive && Severity == x && CreatedDate >= cutoff
  && Title.ToLower() == title.ToLower()`, ordered by `CreatedDate desc`, `FirstOrDefaultAsync`) —
  same style as the existing `GetAllAsync` filter chain.
- In `AlertManagementService.CreateAsync`: trim the incoming title (existing behavior), compute
  `cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-windowMinutes)`, call the new
  repository lookup before calling `AddAsync`. If a match is found, skip persistence and return the
  existing alert's `AlertResponse` plus a duplicate flag; otherwise persist as today.
- Introduce a minimal way to carry the duplicate flag from service to controller (implementation
  decision — e.g. a small `CreateAlertResult` wrapper record) since `IAlertService.CreateAsync`
  today only returns `AlertResponse`. Update `IAlertService` and all callers/tests accordingly.
- In `AlertsController.Create`: when the result indicates suppression, set response header
  `X-Duplicate-Suppressed: true` and return `Ok(existingAlert)` (200); otherwise keep the existing
  `CreatedAtRoute(...)` (201) path unchanged.

## Validation Strategy
- `dotnet build AlertService.sln` — full-slice compile across affected projects.
- `dotnet test AlertService.sln --no-build` — covers controller (200 vs 201 + header), service
  (suppression matching logic: same title/severity within window vs. different severity, expired
  window, inactive prior alert), and repository (EF query correctness) test layers.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Story case is SIMPLE: acceptance criteria are precise (exact match keys, exact window
  semantics, exact status codes/header). No `requirement-analysis.md` created; the two
  implementation-time decisions are recorded in `story-context.md` under Unresolved Questions.
