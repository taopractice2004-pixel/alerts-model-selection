# Changes — ALERT-410

## Stage Summary
- Timestamp: 2026-09-30T00:00:00Z
- Command: /implement-story
- Files changed:
  - AlertService.Models/Tag.cs (new)
  - AlertService.Models/Alert.cs
  - AlertService.Common/Constants/AlertConstants.cs
  - AlertService.Data/Interfaces/IAlertRepository.cs
  - AlertService.Data.SQL/AlertDbContext.cs
  - AlertService.Data.SQL/Configurations/TagConfiguration.cs (new)
  - AlertService.Data.SQL/Repositories/AlertRepository.cs
  - AlertService.Data.SQL/Migrations/20260930055414_AddTagsAndAlertTags.cs (new, plus Designer.cs and updated ModelSnapshot)
  - AlertService.DTO/Responses/AlertResponse.cs
  - AlertService.DTO/Requests/AlertQueryRequest.cs
  - AlertService.DTO/Requests/AddTagsRequest.cs (new)
  - AlertService.API/Controllers/AlertsController.cs
  - AlertService.API/Services/AlertManagementService.cs
  - AlertService.API/Services/IAlertService.cs
  - AlertService.API/Services/TagLimitExceededException.cs (new — not anticipated in implementation-cache.json; required to surface the max-10-tags rule as 400 without an existing custom-exception-to-4xx convention)
  - AlertService.API/Mappings/AlertMappingExtensions.cs
  - AlertService.API.Tests/Controllers/AlertsControllerTests.cs
  - AlertService.API.Tests/Services/AlertManagementServiceTests.cs
  - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
  - database/02_AlertServiceDb_Migrations.sql (regenerated, idempotent)
- Description: Added a `Tag` entity with an implicit EF Core many-to-many relationship to `Alert`
  via a new `AlertTags` join table. Added `POST /api/alerts/{id}/tags` and
  `DELETE /api/alerts/{id}/tags/{tag}` endpoints, a `tag` filter on `GET /api/alerts`, and `Tags` in
  `AlertResponse`. Max 10 tags/alert and 1-30 char tag length enforced; tag names deduped
  case-insensitively. DELETE returns 404 for a missing alert or an unassigned tag (single null
  sentinel, consistent with existing `UpdateAsync`/`DeactivateAsync` convention).

## Unresolved-Question Resolutions
- POST /api/alerts/{id}/tags success status: **200 OK** with the updated `AlertResponse` (not 201),
  since the endpoint mutates an existing parent resource rather than creating a new addressable one.
- Max-10-tags violation surfaced as **400** via a new `TagLimitExceededException` caught locally in
  `AlertsController.AddTags`, returning a `ProblemDetails` body. The global
  `ExceptionHandlingMiddleware` (which always returns 500) was intentionally left unchanged to avoid
  broadening scope; this local catch is the minimal option given there was no existing
  exception-to-4xx convention.

## Validation Results
- `dotnet build AlertService.sln` — succeeded (all 8 projects).
- `dotnet test AlertService.sln --no-build` — 76/76 passed.
- `dotnet ef migrations add AddTagsAndAlertTags --project AlertService.Data.SQL --startup-project AlertService.API` — succeeded.
- `dotnet ef migrations script --idempotent ... --output database/02_AlertServiceDb_Migrations.sql` — regenerated to keep the manual-deploy script in sync with the new migration.

## Coverage Results
- NOT_CONFIGURED (no coverage tooling in repo; behavior coverage added via new unit tests for
  add/remove tags, dedupe, max-tags limit, tag query filter, and response tag mapping).

## Reproduction Results
- NOT_RUN (no reproduction commands defined for this story).

## Standards Applied
- coding-standards.md, backend-dotnet-standards.md, api-rest-standards.md,
  service-architecture-standards.md, database-standards.md.

## Deferred Items
- None.
