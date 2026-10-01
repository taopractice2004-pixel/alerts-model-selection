# Requirement Analysis: ALERT-411

Classified `AMBIGUOUS`: acceptance criteria state required behavior clearly but leave
implementation-shape decisions open. Resolved as follows (also recorded in
`implementation-cache.json` → `design_decisions`):

## Resolved Decisions
1. **Signaling 200-vs-201 to the controller**: new `CreateAlertStatus { Created,
   DuplicateSuppressed }` enum + `CreateAlertResult(Status, Alert)` record on `IAlertService`,
   mirroring the existing `AddAlertTagsResult`/`AddAlertTagsStatus` pattern added for ALERT-410.
   Rejected alternative: throwing/catching an exception for the suppressed case — rejected
   because it conflicts with the repo's existing nullable/result-object convention for
   non-exceptional "alternate outcome" flows (see `UpdateAsync`/`DeactivateAsync` returning
   `null`, `AddTagsAsync` returning a result record).
2. **Configuration source**: standard ASP.NET Core `IOptions<T>` pattern
   (`AlertSuppressionOptions.DuplicateWindowMinutes`, bound from a new
   `AlertSuppression:DuplicateWindowMinutes` section in `appsettings.json`, default `15`).
   Rejected alternative: reading `IConfiguration` directly inside `AlertManagementService` (the
   one existing precedent, in `Program.cs`) — rejected because it is harder to unit test and the
   `service-architecture` standard calls for configuring DI correctly; `IOptions<T>` is
   injectable and mockable in `AlertManagementServiceTests`.
3. **Duplicate lookup location**: new narrow `IAlertRepository.FindActiveDuplicateAsync` method
   rather than reusing/overloading `GetAllAsync`. Rejected alternative: having
   `AlertManagementService` call `GetAllAsync` with filters and inspect the result in memory —
   rejected because it would fetch paged/sorted data unrelated to this exact predicate and
   duplicate filtering logic outside the repository layer, which owns query construction per
   `repository-map.md`.
4. **Header name**: `AlertConstants.DuplicateSuppressedHeaderName = "X-Duplicate-Suppressed"`,
   consistent with the repo's existing small-shared-constant convention in `AlertConstants.cs`.
5. **Time math**: cutoff computed from the injected `TimeProvider` (already used for
   `CreatedDate` on create) minus the configured window, never a hardcoded `TimeSpan`.

## Still Flagged (see implementation-cache.json → missing_facts)
- Default value presence for `DuplicateWindowMinutes` in committed `appsettings.json` (resolved
  conservatively as `15`; confirm during `/implement-story` if repo config conventions differ).
- Single-vs-multiple duplicate match handling (resolved: most-recent match via
  `OrderByDescending(CreatedDate)` + `FirstOrDefault`).
