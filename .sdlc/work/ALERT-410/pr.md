# ALERT-410: Add alert tagging with tag endpoints and filter support

## PR Description
Implement alert tagging end-to-end by adding tag persistence, add/remove tag API endpoints, optional tag filtering on alert list queries, and tag projection in API responses.

This change preserves existing alert behavior while extending the current alert pipeline with bounded validation and relationship management for tags.

## Story / Requirement Summary
- AC1: Add Tag concept with many-to-many Alert relationship and migration/schema updates.
- AC2: Add POST /api/alerts/{id}/tags with case-insensitive dedupe, max 10 tags per alert, and tag length validation (1-30).
- AC3: Add DELETE /api/alerts/{id}/tags/{tag} with 404 when alert is missing or tag is not assigned.
- AC4: Add optional tag filter to GET /api/alerts and compose with existing filters.
- AC5: Include tags in alert response payloads.

## Implementation Summary
- Added controller endpoints for tag add/remove operations and wired them to service methods.
- Extended service contracts and implementations to validate, normalize, deduplicate, and enforce tag limits.
- Extended repository contracts and SQL implementation for tag assignment/removal and composed filtering.
- Added/updated EF Core model/configuration and migration artifacts for Tag and AlertTag entities.
- Added tag data projection into response mappings and response DTOs.

## Changed-Files Summary
Changed-files source: git status --porcelain

- Modified tracked files: 16
- Untracked items reported: 10 (including .sdlc/, .github/, standards/, and new tagging files)

Primary tagging-related changes include:
- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.API/Mappings/AlertMappingExtensions.cs
- AlertService.DTO/Requests/AlertQueryRequest.cs
- AlertService.DTO/Requests/AddAlertTagsRequest.cs
- AlertService.DTO/Responses/AlertResponse.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.Data.SQL/AlertDbContext.cs
- AlertService.Data.SQL/Configurations/TagConfiguration.cs
- AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs
- AlertService.Data.SQL/Migrations/20261005180717_AddAlertTags.cs
- AlertService.Data.SQL/Migrations/20261005180717_AddAlertTags.Designer.cs
- AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs
- AlertService.Models/Tag.cs
- AlertService.Models/AlertTag.cs
- AlertService.Models/Alert.cs
- AlertService.Common/Constants/AlertConstants.cs
- database/02_AlertServiceDb_Migrations.sql

## Acceptance Criteria Traceability
- AC1
  - Implementation: AlertService.Models/Tag.cs, AlertService.Models/AlertTag.cs, AlertService.Data.SQL/AlertDbContext.cs, AlertService.Data.SQL/Configurations/TagConfiguration.cs, AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs, AlertService.Data.SQL/Migrations/20261005180717_AddAlertTags.cs
  - Validation: AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- AC2
  - Implementation: AlertService.API/Controllers/AlertsController.cs, AlertService.API/Services/IAlertService.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Data/Interfaces/IAlertRepository.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs, AlertService.DTO/Requests/AddAlertTagsRequest.cs
  - Validation: AlertService.API.Tests/Controllers/AlertsControllerTests.cs, AlertService.API.Tests/Services/AlertManagementServiceTests.cs, AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- AC3
  - Implementation: AlertService.API/Controllers/AlertsController.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs
  - Validation: AlertService.API.Tests/Controllers/AlertsControllerTests.cs, AlertService.API.Tests/Services/AlertManagementServiceTests.cs, AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- AC4
  - Implementation: AlertService.DTO/Requests/AlertQueryRequest.cs, AlertService.API/Services/AlertManagementService.cs, AlertService.Data/Interfaces/IAlertRepository.cs, AlertService.Data.SQL/Repositories/AlertRepository.cs
  - Validation: AlertService.API.Tests/Services/AlertManagementServiceTests.cs, AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- AC5
  - Implementation: AlertService.DTO/Responses/AlertResponse.cs, AlertService.API/Mappings/AlertMappingExtensions.cs
  - Validation: AlertService.API.Tests/Services/AlertManagementServiceTests.cs, AlertService.API.Tests/Controllers/AlertsControllerTests.cs

## Test / Build Results Already Recorded
- Build: dotnet build AlertService.sln -> PASSED (recorded in implementation stage).
- Unit tests:
  - dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj -> PASSED (52/52)
  - dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj -> PASSED (30/30)
- Coverage:
  - API coverage collected with XPlat Code Coverage.
  - SQL test project coverage collector reported NOT_CONFIGURED for XPlat collector availability.

## Configuration / Database / Migration Impacts
- Database schema impact: Yes.
- Added migration artifacts for tag tables/relations and updated SQL migration script:
  - AlertService.Data.SQL/Migrations/20261005180717_AddAlertTags.cs
  - AlertService.Data.SQL/Migrations/20261005180717_AddAlertTags.Designer.cs
  - AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs
  - database/02_AlertServiceDb_Migrations.sql
- Runtime configuration impact: None.

## Known Risks / Limitations
- Request contract assumption for POST tags uses object wrapper payload: { "tags": ["..."] }.
- Canonical response casing for tags when inputs vary only by case remains an explicit assumption captured during analysis.
