# Changes — ALERT-410

## Summary
Implemented end-to-end alert tagging: a first-class `Tag` entity with an EF-managed
many-to-many join table `AlertTags`, add/remove tag endpoints, a composable `tag` filter on
`GET /api/alerts`, and `Tags` surfaced in `AlertResponse`. Scope matches the approved
`implementation-plan.md` / `implementation-cache.json`; no unrelated refactoring.

## Files Created
- `AlertService.Models/Tag.cs` — `Tag` entity (`Id`, `Name`, `ICollection<Alert> Alerts`).
- `AlertService.DTO/Requests/AddTagsRequest.cs` — `List<string> Tags` with `IValidatableObject`
  (≥1 tag; each 1–30 chars after trim; empties rejected), consistent with existing request DTOs.
- `AlertService.Data.SQL/Configurations/TagConfiguration.cs` — table `Tags`, `Name` required
  max 30, unique index `IX_Tags_Name`.
- `AlertService.API/Services/AlertValidationException.cs` — business-rule signal mapped to 400.
- `AlertService.Data.SQL/Migrations/20261001110000_AddAlertTags.cs` (+ `.Designer.cs`) —
  hand-authored EF 8 migration (see Deviations).

## Files Updated
- `AlertService.Models/Alert.cs` — added `ICollection<Tag> Tags` navigation.
- `AlertService.Common/Constants/AlertConstants.cs` — `MaxTagsPerAlert=10`, `TagMinLength=1`,
  `TagMaxLength=30`.
- `AlertService.DTO/Requests/AlertQueryRequest.cs` — optional `Tag` filter (`StringLength` 30).
- `AlertService.DTO/Responses/AlertResponse.cs` — added `Tags` (ordered list of names).
- `AlertService.Data/Interfaces/IAlertRepository.cs` — `GetAllAsync` gains `string? tag`; added
  `AddTagsAsync` / `RemoveTagAsync`; `GetByIdAsync` now loads Tags.
- `AlertService.Data.SQL/AlertDbContext.cs` — `DbSet<Tag> Tags`.
- `AlertService.Data.SQL/Configurations/AlertConfiguration.cs` — many-to-many via join table
  `AlertTags`.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs` — `AsNoTracking().Include(a => a.Tags)`
  on list/by-id, case-insensitive `tag` filter, tag add (reuse-or-create canonical row) / remove
  persistence.
- `AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs` — model state for `Tag` +
  `AlertTag` join.
- `database/02_AlertServiceDb_Migrations.sql` — idempotent mirror of `Tags`, `AlertTags`, FKs,
  indexes, and history row for `20261001110000_AddAlertTags`.
- `AlertService.API/Services/IAlertService.cs` + `AlertManagementService.cs` — `AddTagsAsync` /
  `RemoveTagAsync`; trim, case-insensitive dedup, max-10 enforced in the service (no EF reference).
- `AlertService.API/Controllers/AlertsController.cs` — `POST {id:int}/tags` (200/400/404),
  `DELETE {id:int}/tags/{tag}` (204/404).
- `AlertService.API/Mappings/AlertMappingExtensions.cs` — projects `alert.Tags` to an ordered
  name list.
- `AlertService.API/Middleware/ExceptionHandlingMiddleware.cs` — maps `AlertValidationException`
  → 400 ProblemDetails with a safe message.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` — updated the two
  `GetAllAsync` mock setups/verifies for the new `tag` parameter (contract-compile only; no new
  feature tests — that is the `/unit-testing` stage).

## Dependencies
- None added.

## Security Notes
- Tag input validated at the trust boundary (DTO length/empty rules + service dedup/limit).
- Repository uses parameterized EF LINQ only (no string-built SQL); no injection surface.
- Error handling returns safe ProblemDetails messages; no internal details leaked. No secrets.

## Validation
- Commands (`dotnet build`, `dotnet test`): **NOT_RUN** — command execution is blocked in this
  environment (no approver available; not an OS/sandbox error). Changes were verified by code
  review instead: all layers compile-consistent, existing `GetAllAsync` test call-sites use
  named/defaulted args and remain compatible, and the migration/snapshot/SQL mirror match EF 8
  output.
- Recommended: run `dotnet build` then `dotnet test` (targeting `AlertService.API.Tests` and
  `AlertService.Data.SQL.Tests`). If a later `dotnet ef migrations add` reports a model diff on
  the hand-authored join-entity snapshot (`AlertTag`), run it once to let EF normalize.

## Assumptions / Deviations
- **Canonical casing:** tags stored with first-seen casing; dedup/matching/uniqueness are
  case-insensitive (service dedups via `ToLowerInvariant`; repository reuses existing rows via
  `Name.ToLower()`; unique `IX_Tags_Name` is the DB backstop under default CI collation). `Tag`
  kept as `(Id, Name)` per AC; "normalized Name" realized as app-level canonicalization + unique
  index (no separate `NormalizedName` column).
- **400 path (beyond literal file list):** added `AlertValidationException` + middleware handling
  so the EF-free service can signal the max-10 business rule as 400 without leaking internals.
- **Hand-authored migration:** `dotnet ef` could not run in this environment, so the migration,
  Designer, and ModelSnapshot were hand-authored to match EF 8 generated output.
