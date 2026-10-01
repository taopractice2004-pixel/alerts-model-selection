# Impact Map: ALERT-410

## Direct Impact (see implementation-cache.json for the exact file list)
- `AlertService.Models` — new `Tag`/`AlertTag` entities, `Alert.Tags` navigation.
- `AlertService.Data` / `AlertService.Data.SQL` — repository contract + EF configuration +
  migration + `AlertDbContext` DbSets.
- `AlertService.DTO` — new request DTO, query/response DTO changes.
- `AlertService.Common` — new tag-related constants.
- `AlertService.API` — controller, service, and mapping changes.

## Adjacent/Out-of-Scope (not touched)
- `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`, `Middleware/`,
  `Program.cs` — no DI/pipeline changes expected; `AddSqlDataAccess` registration stays as-is
  since no new services are introduced beyond existing repository/DbContext.
- `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql` — only touched if
  the repo's convention (TO_BE_DISCOVERED, see `implementation-cache.json`) requires regenerating
  the idempotent script after adding the migration.
- Existing `Alert` CRUD endpoints (`GetById`, `Create`, `Update`, `Deactivate`, `Delete`) —
  behavior unchanged except `AlertResponse` gaining a `Tags` field.

## Downstream Consumers
- Any client deserializing `AlertResponse` gains a new `Tags` field (additive, non-breaking for
  JSON consumers).
