# Implementation Plan — ALERT-410

## Change Strategy
- Add `Tag` entity and an explicit `AlertTag` join entity; wire a many-to-many `Alert.Tags`
  navigation through EF configuration and a new migration. Keep EF entirely inside
  `AlertService.Data.SQL`.
- Extend `IAlertRepository` with tag persistence (add/remove, reuse-by-name) and a `tag` filter
  parameter on `GetAllAsync`; implement in `AlertRepository` using `Include` for tag reads and
  case-insensitive matching. No `SELECT *`.
- Add `AddTagsAsync` / `RemoveTagAsync` to `IAlertService` + `AlertManagementService` enforcing
  dedupe, the max-10 cap, and 1–30 char bounds; return `AlertResponse?` (null → 404) and a
  remove result that distinguishes missing alert/assignment for the 404 path.
- Surface two controller actions on `AlertsController` and add the `Tag` query field to
  `AlertQueryRequest`; map `Tags` in `AlertMappingExtensions`. Validation via data annotations +
  service rules surfaced as RFC7807.
- Add tag length/count constants to `AlertConstants`; append generated migration SQL to
  `database/02_AlertServiceDb_Migrations.sql`.

## Validation Strategy
- `dotnet build AlertService.sln` then `dotnet test` — the existing controller, service, and
  repository test projects cover the changed layers, so the full solution test run is the
  narrowest sufficient gate. Add focused unit tests for dedupe, cap, bounds, 404 paths, and tag
  filter composition.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Scope is intentionally broader than the default 5-file cap because this is a genuine
  cross-layer feature (model → data → service → API → DTO → migration).
