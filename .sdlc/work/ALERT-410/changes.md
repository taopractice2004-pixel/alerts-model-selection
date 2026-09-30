# Changes — ALERT-410

## Stage Summary
- Timestamp: 2026-09-30
- Command: /implement-story
- Files changed:
  - `AlertService.Models/Tag.cs` (new)
  - `AlertService.Models/AlertTag.cs` (new explicit join entity)
  - `AlertService.Models/Alert.cs` (add `AlertTags` navigation)
  - `AlertService.Common/Constants/AlertConstants.cs` (tag count/length constants)
  - `AlertService.DTO/Responses/AlertResponse.cs` (add `Tags`)
  - `AlertService.DTO/Requests/AddTagsRequest.cs` (new)
  - `AlertService.DTO/Requests/AlertQueryRequest.cs` (add `Tag` filter)
  - `AlertService.Data/Interfaces/IAlertRepository.cs` (tag param + tag ops)
  - `AlertService.Data.SQL/Repositories/AlertRepository.cs` (filter, eager load, add/remove)
  - `AlertService.Data.SQL/Configurations/TagConfiguration.cs` (new)
  - `AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs` (new)
  - `AlertService.Data.SQL/AlertDbContext.cs` (`DbSet<Tag>`, `DbSet<AlertTag>`)
  - `AlertService.Data.SQL/Migrations/20260930031525_AddAlertTags*.cs` (new EF migration)
  - `AlertService.API/Services/IAlertService.cs` (tag ops + result types)
  - `AlertService.API/Services/AlertManagementService.cs` (dedupe/cap/bounds orchestration)
  - `AlertService.API/Controllers/AlertsController.cs` (POST/DELETE tag endpoints)
  - `AlertService.API/Mappings/AlertMappingExtensions.cs` (map `Tags`, ordered ascending)
  - `database/02_AlertServiceDb_Migrations.sql` (regenerated idempotent script)
  - Tests: `AlertsControllerTests.cs`, `AlertManagementServiceTests.cs`, `AlertRepositoryTests.cs`
- Description: Implemented free-form Tag concept with an explicit many-to-many `AlertTag` join.
  Added `POST /api/alerts/{id}/tags` (dedupe case-insensitively, reuse existing Tag rows,
  enforce max-10 cap → 400, 404 when alert missing) and `DELETE /api/alerts/{id}/tags/{tag}`
  (404 when alert or assignment missing, 204 on success). Added a composable `tag` filter to
  `GET /api/alerts` and exposed `Tags` (ascending) in `AlertResponse`. EF stays in the data
  layer; read paths eager-load tags via `Include`.

## Validation Results
- `dotnet build AlertService.sln` → Build succeeded, 0 warnings, 0 errors.
- `dotnet test` → Passed: 84 total (54 API + 30 Data.SQL), 0 failed.

## Coverage Results
- Not run this stage; focused behavior tests added for dedupe, max-10 cap, bounds, 404 paths,
  tag reuse, and tag-filter composition.

## Reproduction Results
- NOT_RUN (feature story, not a bug fix).

## Standards Applied
- coding-standards.md, backend-dotnet-standards.md, api-rest-standards.md,
  service-architecture-standards.md, database-standards.md (no `SELECT *`; eager-load via
  `Include`; explicit join entity; unique `IX_Tags_Name`; RFC7807 validation responses).

## Deferred Items
- None.
