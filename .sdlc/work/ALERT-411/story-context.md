# Story Context: ALERT-411

## Summary
Suppress near-duplicate alerts on create: when an *active* alert with the same `Title`
(case-insensitive) and the same `Severity` was created within a configurable window (default 15
minutes), `POST /api/alerts` returns `200 OK` with the existing alert and header
`X-Duplicate-Suppressed: true` instead of creating a new row. A genuinely new alert still returns
`201 Created`. See `implementation-cache.json` for the full acceptance criteria list, exact
files, and resolved design decisions — not duplicated here.

## Business Context
- Operator-facing noise-reduction capability layered onto the existing
  `AlertService.API/Controllers/AlertsController.cs` create flow.
- No external tracker; story details were supplied directly by the user for this
  `/analyze-story` invocation.

## Relationship To Existing Work
- Builds directly on the existing `Alert` create flow (`AlertManagementService.CreateAsync`,
  `IAlertRepository`, `AlertRepository`) rather than introducing a parallel pattern.
- Reuses the result/status pattern already established by `AddAlertTagsResult`/
  `AddAlertTagsStatus` (added for ALERT-410) for signaling a secondary outcome (here:
  "suppressed" vs. "created") back to the controller.
- First story in this repo to introduce strongly-typed configuration (`IOptions<T>`); prior code
  only reads raw `IConfiguration` once, in `Program.cs`, for a boot-time flag.

## Unresolved/Flagged Items
See `implementation-cache.json` → `missing_facts` for the two `TO_BE_DISCOVERED` items (default
value presence for `DuplicateWindowMinutes`; single-vs-multiple duplicate match handling).
