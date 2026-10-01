# Impact Map — ALERT-410

## Primary Slice
- Alert API + service + repository query/update path for `/api/alerts` and new tag assignment routes.

## Adjacent Dependencies
- Domain/model and DTO mapping surface for `Alert`/`AlertResponse`.
- EF Core model configuration and SQL migrations under `AlertService.Data.SQL/Migrations/`.
- Shared constants for tag validation limits/pattern constraints.

## Out Of Scope
- Changes to unrelated summary/count semantics.
- Frontend/UI changes.
- External search/index systems.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
