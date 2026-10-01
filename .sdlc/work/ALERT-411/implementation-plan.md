# Implementation Plan — ALERT-411

## Change Strategy
Ordered, minimal slice following existing layering (config -> Data interface -> Data.SQL repo ->
API service -> API controller):
1. **Config** — add `AlertSuppression: { "WindowMinutes": 15 }` to `appsettings.json`; add
   strongly-typed `AlertSuppressionOptions` (new) and bind/register it in `Program.cs`. Window
   read from config, not hardcoded (code fallback default of 15 acceptable if config missing).
2. **Data interface** — add `Task<Alert?> FindActiveDuplicateAsync(string title, Severity severity,
   DateTime createdOnOrAfterUtc, CancellationToken ct)` to `IAlertRepository`.
3. **Data.SQL repository** — implement the lookup: `IsActive == true`, same `Severity`,
   case-insensitive `Title` equality (existing `.ToLower()` EF pattern), `CreatedDate >=
   createdOnOrAfterUtc`; return a deterministic match (most recent) or null. EF stays in repo.
4. **API service** — `CreateAsync` returns new `AlertCreateResult` (`AlertResponse` +
   `WasSuppressed`). Compute `createdOnOrAfterUtc = _timeProvider.GetUtcNow().UtcDateTime - window`
   from bound options; call duplicate lookup first — if found, return existing mapped to
   `AlertResponse` with `WasSuppressed=true` (no `AddAsync`); else create as today with
   `WasSuppressed=false`. Trim Title consistent with existing `ToEntity` trimming before compare.
5. **Controller** — `AlertsController.Create`: if suppressed, set header
   `X-Duplicate-Suppressed: true` and return `Ok(result.Alert)` (200); else
   `CreatedAtRoute(...)` (201). Update `[ProducesResponseType]` to document 200 + 201.
6. **Negative cases** — different Severity, inactive prior, or outside window => not a duplicate
   (create new, 201).

## Validation Strategy
- `dotnet build` then `dotnet test` (targeted `AlertService.API.Tests` and
  `AlertService.Data.SQL.Tests` runs acceptable) — covers controller status/header, service
  suppressed-vs-created logic, and repository duplicate-query behavior. Coverage: NOT_CONFIGURED.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Slice is intentionally broader than the 5-file soft cap: the feature legitimately spans API,
  Data, and config layers end-to-end.
- No DB schema change / EF migration needed (no new columns).
