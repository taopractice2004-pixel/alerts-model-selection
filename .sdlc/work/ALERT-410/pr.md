# ALERT-410: Add alert tagging endpoints and filtering

## PR Description
Implement free-form alert tagging end to end so operators can attach, remove, and filter alerts by tag. This change adds the backing tag schema, the alert tag mutation endpoints, tag-aware query filtering, tag projection in responses, and focused unit coverage for the affected controller, service, and repository slices.

## Story / Requirement Summary
- AC1: Persist a new `Tag` concept for alerts with a many-to-many relationship and a database migration for the tag and join-table schema.
- AC2: `POST /api/alerts/{id}/tags` adds one or more tags, deduplicates case-insensitively, rejects blank or overlength tags, and enforces a maximum of 10 tags per alert.
- AC3: `DELETE /api/alerts/{id}/tags/{tag}` removes a tag assignment and returns 404 when the alert or assignment does not exist.
- AC4: `GET /api/alerts` supports an optional `tag` filter that composes with the existing alert filters.
- AC5: `AlertResponse` includes alert tags for single-item and paged responses.

## Implementation Summary
- Added `Tag` persistence with EF Core model/configuration updates, repository support, and a migration for the new tag and join-table schema.
- Extended the alert service and controller with tag add/remove operations and preserved the existing controller -> service -> repository flow.
- Added case-insensitive tag deduplication, removal, and list filtering behavior in the repository/service path.
- Updated request/response DTOs and alert mapping so tags are accepted by the new POST endpoint and returned from alert reads.
- Added focused unit coverage for controller validation and endpoints, service business rules, and repository persistence/filter behavior.

## Changed-Files Summary
Changed-files source: `git show --stat --name-only --format= HEAD` was available but did not isolate the story-scoped implementation files for this work item, so the story-scoped summary below is taken from `work.json` and the recorded implementation/unit-testing log entries.

Production and data changes:
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Mappings/AlertMappingExtensions.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.Common/Constants/AlertConstants.cs`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/AlertDbContext.cs`
- `AlertService.Data.SQL/Configurations/AlertConfiguration.cs`
- `AlertService.Data.SQL/Configurations/TagConfiguration.cs`
- `AlertService.Data.SQL/Migrations/20261005120000_AddAlertTags.cs`
- `AlertService.Data.SQL/Migrations/20261005120000_AddAlertTags.Designer.cs`
- `AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.DTO/Requests/AddAlertTagsRequest.cs`
- `AlertService.DTO/Requests/AlertQueryRequest.cs`
- `AlertService.DTO/Responses/AlertResponse.cs`
- `AlertService.Models/Alert.cs`
- `AlertService.Models/Tag.cs`

Focused tests:
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

Workflow artifacts:
- `.sdlc/work/ALERT-410/work.json`
- `.sdlc/work/ALERT-410/log.md`

## Acceptance Criteria Traceability
| AC | Implementation | Validation |
|---|---|---|
| AC1 | `AlertService.Models/Tag.cs`, `AlertService.Models/Alert.cs`, `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Configurations/TagConfiguration.cs`, `AlertService.Data.SQL/Migrations/20261005120000_AddAlertTags.cs` | `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |
| AC2 | `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.DTO/Requests/AddAlertTagsRequest.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |
| AC3 | `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |
| AC4 | `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.DTO/Requests/AlertQueryRequest.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` |
| AC5 | `AlertService.API/Mappings/AlertMappingExtensions.cs`, `AlertService.DTO/Responses/AlertResponse.cs`, `AlertService.Models/Alert.cs` | `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` |

## Test / Build Results Already Recorded
- Build: `dotnet build AlertService.API/AlertService.API.csproj` -> SUCCESS
- Unit tests: `78/78` passed across the scoped controller, service, and repository suites.
- Coverage: API scoped coverage generated at `AlertService.API.Tests/TestResults/c9154a0a-bf7a-4ffe-95b2-8db307030d13/coverage.cobertura.xml`.
- Coverage limitation: repository coverage is `NOT_CONFIGURED` because `XPlat Code Coverage` is not available in `AlertService.Data.SQL.Tests`.

## Configuration / Database / Migration Impacts
- Database schema change: adds tag persistence and the alert-tag join table via `AlertService.Data.SQL/Migrations/20261005120000_AddAlertTags.cs`.
- API surface change: adds `POST /api/alerts/{id}/tags` and `DELETE /api/alerts/{id}/tags/{tag}` and extends `GET /api/alerts` with the optional `tag` filter.

## Known Risks / Limitations
None.