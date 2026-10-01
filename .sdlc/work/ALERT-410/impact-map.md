# Impact Map — ALERT-410

## Primary Slice
- Alert tagging feature end-to-end: `Tag` entity + `AlertTags` many-to-many, tag add/remove
  endpoints on `AlertsController`, `tag` filter on `GET /api/alerts`, and Tags in `AlertResponse`.

## Adjacent Dependencies
- New EF migration under `AlertService.Data.SQL/Migrations/` plus regenerated ModelSnapshot.
- `AlertDbContext` (`DbSet<Tag>` + auto-applied configuration).
- Idempotent SQL mirror `database/02_AlertServiceDb_Migrations.sql`.

## Out Of Scope
- Existing alert CRUD/list endpoints and `GetSummaryAsync` behavior remain unchanged except that
  Tags are now included in `AlertResponse`.
- No new third-party dependencies; no changes to severity/paging/sorting semantics.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
