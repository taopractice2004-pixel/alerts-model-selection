# Implementation Plan: ALERT-410

Authoritative file inventory, validation commands, and design decisions live in
`implementation-cache.json`. This plan sequences the work; it does not repeat those lists.

## Sequencing
1. **Domain + data layer**
   - Add `Tag` and `AlertTag` entities in `AlertService.Models`; add `Alert.Tags` navigation.
   - Add `TagConfiguration` / `AlertTagConfiguration` (composite PK, unique index on `Tag.Name`,
     FKs from `AlertTag` to `Alert`/`Tag`), register new `DbSet`s in `AlertDbContext`.
   - Generate the EF Core migration (`dotnet ef migrations add AddAlertTags`).
2. **Repository contract + implementation**
   - Extend `IAlertRepository` with a `tag` filter parameter on `GetAllAsync` and new methods to
     find-or-create tags, attach tags to an alert, and remove a single tag from an alert
     (case-insensitive name match).
   - Implement in `AlertRepository`, including `Include(a => a.Tags)` so responses can surface
     tag names without N+1 queries.
3. **DTO + mapping**
   - Add `AddAlertTagsRequest` (list of 1-10 strings, each 1-30 chars).
   - Add `Tag` to `AlertQueryRequest`.
   - Add `Tags` (`IReadOnlyList<string>`) to `AlertResponse`; update
     `AlertMappingExtensions.ToResponse`.
4. **Service layer**
   - `AlertManagementService`: validate the 10-tags-per-alert cap and 1-30 char length,
     case-insensitively de-duplicate against the alert's existing tags, return `null`/404 markers
     consistent with existing `UpdateAsync`/`DeactivateAsync` patterns when the alert is missing.
5. **Controller**
   - `POST /api/alerts/{id}/tags` → 200 with updated `AlertResponse`, 400 on validation failure,
     404 if the alert does not exist.
   - `DELETE /api/alerts/{id}/tags/{tag}` → 204, 404 if the alert or the tag assignment does not
     exist.
   - `GET /api/alerts` → thread the new `Tag` query parameter through unchanged otherwise.
6. **Tests**
   - Extend `AlertRepositoryTests`, `AlertManagementServiceTests`, `AlertsControllerTests` for:
     add/dedupe/cap/length validation, delete success/404 cases, and the `tag` filter composed
     with existing filters.
7. **Validation**
   - `dotnet build AlertService.sln`, `dotnet test`.

## Risks
- Join-table migration must not break the existing `AlertConfiguration`/`InitialCreate`
  migration chain — add, don't edit, prior migrations.
- Case-insensitive tag matching must behave the same across SQL Server (production) and the
  Sqlite/InMemory providers used in `AlertService.Data.SQL.Tests` (flagged as `TO_BE_DISCOVERED`
  in `implementation-cache.json`).
