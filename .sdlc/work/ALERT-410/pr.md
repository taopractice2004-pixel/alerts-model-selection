# ALERT-410: Add alert tagging support

## PR Description
Add free-form tagging to alerts across the API, service, repository, and EF Core schema layers. This change adds tag assignment and removal endpoints, tag-filtered alert queries, and tag projection in alert responses while preserving the existing controller-to-repository seam.

## Story / Requirement Summary
- AC1: Add a Tag concept with a many-to-many Alert relationship, including persistence and migration artifacts.
- AC2: Add `POST /api/alerts/{id}/tags` to assign one or more tags, dedupe case-insensitively, enforce a 10-tag maximum per alert, and validate tag length from 1 to 30 characters.
- AC3: Add `DELETE /api/alerts/{id}/tags/{tag}` to remove a tag assignment and return not found when the alert or tag assignment does not exist.
- AC4: Add an optional `tag` query filter to `GET /api/alerts` that composes with existing filters.
- AC5: Include alert tags in `AlertResponse` list and detail payloads.

## Implementation Summary
- Added tag request/response shape updates and tag-specific service contracts to support alert tagging workflows.
- Extended the alert domain and EF Core model with a many-to-many Alert/Tag relationship, repository add/remove behavior, and migration artifacts.
- Added controller endpoints and service logic for tag assignment, validation, normalization, dedupe, removal, and response projection.
- Extended the existing unit test suites at the controller, service, and repository layers to prove the tagging behavior end to end within the scoped seams.

## Changed-Files Summary
Source: `git status --porcelain` for the current worktree, reconciled against `work.json` scoped files.

Implementation files:
- `AlertService.Common/Constants/AlertConstants.cs`
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.API/Services/AlertTagAddResult.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.API/Mappings/AlertMappingExtensions.cs`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/AlertDbContext.cs`
- `AlertService.Data.SQL/Configurations/AlertConfiguration.cs`
- `AlertService.Data.SQL/Configurations/TagConfiguration.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.Data.SQL/Migrations/20261005000100_AddAlertTags.cs`
- `AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs`
- `AlertService.DTO/Requests/AddAlertTagsRequest.cs`
- `AlertService.DTO/Requests/AlertQueryRequest.cs`
- `AlertService.DTO/Responses/AlertResponse.cs`
- `AlertService.Models/Alert.cs`
- `AlertService.Models/Tag.cs`

Test files:
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

SDLC artifacts:
- `.sdlc/work/ALERT-410/work.json`
- `.sdlc/work/ALERT-410/plan.md`
- `.sdlc/work/ALERT-410/log.md`

## Acceptance Criteria Traceability
- AC1 -> Implemented in `AlertService.Models/Tag.cs`, `AlertService.Models/Alert.cs`, `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Configurations/AlertConfiguration.cs`, `AlertService.Data.SQL/Configurations/TagConfiguration.cs`, and `AlertService.Data.SQL/Migrations/20261005000100_AddAlertTags.cs`; validated by `AddTagsAsync_CreatesNewTags_AssignsThem_AndSkipsDuplicates`, `AddTagsAsync_ReusesExistingTagEntityAcrossAlerts`, `RemoveTagAsync_RemovesAssignment_AndDeletesOrphanedTag`, and `GetByIdAsync_WhenExists_ReturnsAlert` in `AlertRepositoryTests`.
- AC2 -> Implemented in `AlertService.DTO/Requests/AddAlertTagsRequest.cs`, `AlertService.Common/Constants/AlertConstants.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.API/Services/AlertTagAddResult.cs`, and `AlertService.API/Controllers/AlertsController.cs`; validated by `AddAlertTagsRequest_WithInvalidTags_FailsValidation`, `AddTags_WhenSuccessful_ReturnsOkWithUpdatedAlert`, `AddTags_WhenAlertMissing_ReturnsNotFound`, `AddTags_WhenValidationFails_ReturnsValidationProblem`, `AddTagsAsync_WhenNoUsableTags_ReturnsValidationError`, `AddTagsAsync_DedupesCaseInsensitively_AndReturnsUpdatedResponse`, and `AddTagsAsync_WhenTotalUniqueTagsExceedsLimit_ReturnsValidationError`.
- AC3 -> Implemented in `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, and `AlertService.Data.SQL/Repositories/AlertRepository.cs`; validated by `DeleteTag_WhenFound_ReturnsNoContent`, `DeleteTag_WhenMissing_ReturnsNotFound`, `DeleteTag_WithInvalidTag_ReturnsValidationProblem`, `RemoveTagAsync_NormalizesTagBeforeRemoving`, `RemoveTagAsync_RemovesAssignment_AndDeletesOrphanedTag`, `RemoveTagAsync_WhenTagSharedAcrossAlerts_OnlyRemovesAssignment`, and `RemoveTagAsync_WhenAlertOrTagMissing_ReturnsFalse`.
- AC4 -> Implemented in `AlertService.DTO/Requests/AlertQueryRequest.cs`, `AlertService.API/Services/AlertManagementService.cs`, and `AlertService.Data.SQL/Repositories/AlertRepository.cs`; validated by `GetAllAsync_PassesQueryOptionsToRepository` and `GetAllAsync_WithTag_ComposesWithExistingFilters`.
- AC5 -> Implemented in `AlertService.DTO/Responses/AlertResponse.cs`, `AlertService.API/Mappings/AlertMappingExtensions.cs`, and the updated alert retrieval flow; validated by `GetAllAsync_MapsEntitiesToPagedResponse`, `GetByIdAsync_WhenExists_ReturnsResponse`, and `GetByIdAsync_WhenExists_ReturnsAlert`.

## Test / Build Results Already Recorded
- Build: `dotnet build AlertService.API/AlertService.API.csproj` -> SUCCEEDED during `/implement-story`.
- API unit tests: `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"` -> 48/48 passed.
- Repository unit tests: `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"` -> 31/31 passed.
- Acceptance criteria: AC1 MET, AC2 MET, AC3 MET, AC4 MET, AC5 MET.
- Coverage: `NOT_CONFIGURED`.

## Configuration / Database / Migration Impacts
- Database schema impact: adds tag persistence and the Alert/Tag join relationship through `AlertService.Data.SQL/Migrations/20261005000100_AddAlertTags.cs`.
- Configuration impact: None.

## Known Risks / Limitations
- None.