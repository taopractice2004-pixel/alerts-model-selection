# ALERT-410: Add alert tagging support across API, service, and persistence

## PR Description
Implement alert tagging end to end, including many-to-many persistence, tag management endpoints, tag-aware querying, and tag projection in response models. This change delivers AC1-AC5 for ALERT-410 with unit-test verification already recorded in the work log.

## Story / Requirement Summary
- AC1: Add Tag concept with many-to-many Alert relationship and EF migration support.
- AC2: Add POST /api/alerts/{id}/tags with case-insensitive dedupe, max 10 tags per alert, and per-tag length 1-30.
- AC3: Add DELETE /api/alerts/{id}/tags/{tag} with 404 behavior for missing alert or missing assignment.
- AC4: Add optional tag filter to GET /api/alerts that composes with existing filters.
- AC5: Include tags in AlertResponse where AlertResponse is returned.

## Implementation Summary
- Added Tag entity and Alert-Tag many-to-many persistence configuration, including migration and snapshot updates.
- Extended repository contracts and implementation for tag-aware list/single retrieval and add/remove tag operations.
- Updated service and controller flows for add-tags, remove-tag, and tag-filter handling.
- Extended request/response and mapping layers to carry tag filters and response tag collections.

## Changed Files Summary
Diff source: git diff --name-only (read-only).

Scoped ALERT-410 source files:
- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Mappings/AlertMappingExtensions.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.Common/Constants/AlertConstants.cs
- AlertService.DTO/Requests/AddAlertTagsRequest.cs
- AlertService.DTO/Requests/AlertQueryRequest.cs
- AlertService.DTO/Responses/AlertResponse.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/AlertDbContext.cs
- AlertService.Data.SQL/Configurations/AlertConfiguration.cs
- AlertService.Data.SQL/Configurations/TagConfiguration.cs
- AlertService.Data.SQL/Migrations/20261005151025_AddAlertTags.cs
- AlertService.Data.SQL/Migrations/20261005151025_AddAlertTags.Designer.cs
- AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.Models/Alert.cs
- AlertService.Models/Tag.cs

Scoped ALERT-410 test files:
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

Note: The repository diff also contains unrelated pending workspace changes outside ALERT-410 scope.

## Acceptance Criteria Traceability
- AC1 -> Implemented in: AlertService.Models/Tag.cs, AlertService.Models/Alert.cs, AlertService.Data.SQL/AlertDbContext.cs, AlertService.Data.SQL/Configurations/AlertConfiguration.cs, AlertService.Data.SQL/Configurations/TagConfiguration.cs, AlertService.Data.SQL/Migrations/20261005151025_AddAlertTags.cs. Validated by: AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs.
- AC2 -> Implemented in: AlertService.API/Controllers/AlertsController.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Common/Constants/AlertConstants.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs. Validated by: AlertService.API.Tests/Controllers/AlertsControllerTests.cs and AlertService.API.Tests/Services/AlertManagementServiceTests.cs.
- AC3 -> Implemented in: AlertService.API/Controllers/AlertsController.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs. Validated by: AlertService.API.Tests/Controllers/AlertsControllerTests.cs and AlertService.API.Tests/Services/AlertManagementServiceTests.cs.
- AC4 -> Implemented in: AlertService.DTO/Requests/AlertQueryRequest.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs. Validated by: AlertService.API.Tests/Services/AlertManagementServiceTests.cs and AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs.
- AC5 -> Implemented in: AlertService.DTO/Responses/AlertResponse.cs and AlertService.API/Mappings/AlertMappingExtensions.cs. Validated by: AlertService.API.Tests/Services/AlertManagementServiceTests.cs.

## Build and Test Results Already Recorded
- Build (implementation stage): dotnet build AlertService.sln -> SUCCESS
- Unit tests: AlertService.API.Tests -> SUCCESS (56/56); AlertService.Data.SQL.Tests -> SUCCESS (30/30)
- Coverage: API coverage collector run succeeded with Cobertura output; Data.SQL coverage collector not configured in current test environment.

## Configuration / Database / Migration Impact
- Database schema changed: new tag persistence with many-to-many relationship and join table via EF migration.
- No runtime configuration changes required.

## Known Risks / Limitations
- None recorded in plan.md.