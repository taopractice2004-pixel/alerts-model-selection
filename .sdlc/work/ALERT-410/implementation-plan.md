# Implementation Plan — ALERT-410

## Change Strategy
- Add a `Tag` entity and many-to-many navigation on `Alert`, configured via EF Core fluent config
  (new `TagConfiguration.cs` or extended `AlertConfiguration.cs`) and one new migration.
- Extend `IAlertRepository`/`AlertRepository` with a `tag` filter param on the existing
  `GetAllAsync` query and new methods to add/remove tags for an alert.
- Extend `IAlertService`/`AlertManagementService` with add/remove-tag methods that follow the
  existing nullable/not-found-return convention used by `UpdateAsync`/`DeactivateAsync`/`DeleteAsync`.
- Add `AlertsController` routes: `POST /api/alerts/{id}/tags`, `DELETE /api/alerts/{id}/tags/{tag}`;
  add `Tag` to `AlertQueryRequest`; add a new request DTO for the tag-add body.
- Add `Tags` to `AlertResponse` and extend `AlertMappingExtensions.cs` accordingly.
- Add tag length (1–30) and max-tags-per-alert (10) constants to `AlertConstants.cs`; enforce
  case-insensitive dedupe in the service layer.
- Decide (at implementation time) the success status code for the add-tags endpoint and how the
  "max 10 tags" rule surfaces as a 400 — no existing custom-exception convention to reuse; keep the
  decision minimal and consistent with `api-rest-standards.md`.

## Validation Strategy
- `dotnet build AlertService.sln` — full-slice compile across the 5 affected projects.
- `dotnet test AlertService.sln --no-build` — exercises controller, service, and repository tests
  (Moq-based API tests + EF Core InMemory/Sqlite repository tests), sufficient for this vertical
  slice without needing a narrower per-project run.
- `dotnet ef migrations add <Name> --project AlertService.Data.SQL --startup-project AlertService.API`
  to generate the new join-table migration before running tests that touch persistence.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Story case is SIMPLE: acceptance criteria are precise (exact routes, exact limits, exact 404
  semantics). No `requirement-analysis.md` created.
