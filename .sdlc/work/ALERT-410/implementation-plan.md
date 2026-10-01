# Implementation Plan — ALERT-410

## Change Strategy
Ordered, minimal slice following existing layering (Models -> Data -> Data.SQL -> DTO -> Common
-> API):
1. **Models** — add `Tag` entity (Id, Name) and `ICollection<Tag> Tags` nav on `Alert`.
2. **Data interface** — add `string? tag` param to `GetAllAsync`; add tag add/remove methods;
   ensure `GetByIdAsync` contract loads Tags.
3. **Data.SQL config + DbContext + repository** — add `DbSet<Tag> Tags`; new `TagConfiguration`
   (unique index on normalized Name); configure many-to-many join (`AlertTags`); repository tag
   add/remove persistence, case-insensitive `tag` filter, and `Include(a => a.Tags)` where
   responses are produced (reconcile `AsNoTracking` + `Include`).
4. **EF migration + SQL mirror** — generate a new EF migration for Tags + join table; update the
   idempotent `database/02_AlertServiceDb_Migrations.sql` mirror; ModelSnapshot regenerates.
5. **DTO** — `AlertResponse` gains `Tags`; new `AddTagsRequest` (list, 1-30 chars each, max 10)
   with DataAnnotations style; `AlertQueryRequest` gains optional `Tag` filter composed with
   existing filters.
6. **Common** — add `MaxTagsPerAlert=10`, `TagMinLength=1`, `TagMaxLength=30` to `AlertConstants`.
7. **Service + validation** — `AddTagsAsync` (null if alert missing) and `RemoveTagAsync`
   (distinguish 404 cases); trim, case-insensitive dedup, 1-30 length, max 10 per alert after
   dedup vs existing. Service stays off EF Core.
8. **Controller endpoints** — `POST {id:int}/tags` (200/201/400/404) and
   `DELETE {id:int}/tags/{tag}` (204/404); ensure routing accepts free-form tag value.
9. **Mapping** — `AlertMappingExtensions.ToResponse` projects `alert.Tags` to an ordered list of
   names.

## Validation Strategy
- `dotnet build` then `dotnet test` (targeted runs on `AlertService.API.Tests` and
  `AlertService.Data.SQL.Tests` are acceptable) — covers service validation, controller status
  codes, and repository filter/persistence behavior. Coverage: NOT_CONFIGURED.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Slice is intentionally broader than the 5-file soft cap because the many-to-many feature spans
  all layers end-to-end.
