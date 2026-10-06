# ALERT-410: Add free-form tagging to alerts

## Description
Introduce a free-form `Tag` concept with a many-to-many relationship to `Alert`, add endpoints
to attach and remove tags, add an optional `tag` filter to the alert list, and surface tags in
`AlertResponse`.

## Story / Requirement Summary
Add free-form tagging to alerts: new `Tag` entity with a many-to-many relationship to `Alert`,
add/remove tag endpoints, an optional tag filter on list, and tags in `AlertResponse`.

Acceptance criteria (AC1–AC9):
- **AC1** — New `Tag` concept with a many-to-many relationship to `Alert`, backed by a join table and an EF Core migration.
- **AC2** — `POST /api/alerts/{id}/tags` adds one or more tags and returns the updated alert; 404 when the alert does not exist.
- **AC3** — Adding tags dedupes case-insensitively; re-adding an existing tag is a no-op.
- **AC4** — Maximum of 10 tags per alert; a request that would exceed 10 is rejected with a validation error (400).
- **AC5** — Each tag value must be 1–30 characters; out-of-range tags are rejected (400).
- **AC6** — `DELETE /api/alerts/{id}/tags/{tag}` removes the assignment and returns 204.
- **AC7** — `DELETE /api/alerts/{id}/tags/{tag}` returns 404 when the alert does not exist or the tag is not assigned.
- **AC8** — `GET /api/alerts` accepts an optional `tag` query parameter, composable with existing filters.
- **AC9** — `AlertResponse` includes the alert's tags.

## Implementation Summary
Implemented on the recommended design defaults: a global `Tag` table with an `AlertTag` join
(many-to-many), case-insensitive dedupe, `POST` returns the updated `AlertResponse`, and
case-insensitive filter/delete matching.

- **Models** — new `Tag` entity; `Alert.Tags` navigation.
- **Data (contracts)** — `IAlertRepository` tag add/remove methods and an optional `tag` filter parameter on `GetAllAsync`.
- **Data.SQL** — `AlertRepository` tag add/remove + tag filter with `Include`; `TagConfiguration` (unique case-insensitive index on `Name`, many-to-many via `AlertTag`); `AlertDbContext` wiring; `AddAlertTags` migration (dotnet-ef 8.0.31) + regenerated snapshot; `database/02_AlertServiceDb_Migrations.sql` kept in sync.
- **API** — `AlertManagementService.AddTagsAsync` (dedupe + max-10 → `AddTagsResult`) and `RemoveTagAsync`; `IAlertService` extensions; `AlertsController` `POST /api/alerts/{id}/tags` (200/400/404) and `DELETE /api/alerts/{id}/tags/{tag}` (204/404); `AlertMappingExtensions` tag mapping.
- **DTO** — new `AddTagsRequest` (1–30 char validation, ≥1 tag via `IValidatableObject`); new `AddTagsResult`; `AlertQueryRequest.Tag`; `AlertResponse.Tags`.
- **Common** — constants `TagMinLength`, `TagMaxLength`, `MaxTagsPerAlert`.

## Changed Files (from `git status` + `git diff --stat`)
Production:
- `AlertService.Models/Tag.cs` (new), `AlertService.Models/Alert.cs`
- `AlertService.Common/Constants/AlertConstants.cs`
- `AlertService.DTO/Requests/AddTagsRequest.cs` (new), `AlertService.DTO/Requests/AlertQueryRequest.cs`
- `AlertService.DTO/Responses/AddTagsResult.cs` (new), `AlertService.DTO/Responses/AlertResponse.cs`
- `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/IAlertService.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.API/Mappings/AlertMappingExtensions.cs`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Configurations/TagConfiguration.cs` (new), `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.Data.SQL/Migrations/20261005181531_AddAlertTags.cs` (new) + `.Designer.cs` (new) + `AlertDbContextModelSnapshot.cs`
- `database/02_AlertServiceDb_Migrations.sql`

Tests:
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

Diff source: `git diff --stat HEAD` — 16 tracked files modified (+685/-5), plus 6 new untracked
source files listed above.

## Acceptance Criteria → Implementation / Validation Traceability
| AC | Implemented in | Proven by |
|----|----------------|-----------|
| AC1 | `Tag.cs`, `Alert.Tags`, `TagConfiguration`, `AddAlertTags` migration | `AlertRepositoryTests.AddTagsAsync_CreatesNewTags_AndAssignsToAlert`, `GetByIdAsync_IncludesTags` |
| AC2 | `AlertsController` POST, `AddTagsAsync` | `AlertsControllerTests.AddTags_WhenSuccess_ReturnsOkWithUpdatedAlert`, `AddTags_WhenAlertNotFound_ReturnsNotFound`; `AddTagsAsync_WithNewTags_AddsThemAndReturnsUpdatedAlert`, `_WhenAlertMissing_ReturnsAlertNotFound` |
| AC3 | `AddTagsAsync` dedupe | `AddTagsAsync_DedupesWithinRequestCaseInsensitively`, `_WhenTagAlreadyAssigned_IsNoOp`, `_TrimsAndDropsBlankTags` |
| AC4 | `AddTagsAsync` max-10 → `TagLimitExceeded` | `AddTagsAsync_WhenResultWouldExceedMax_ReturnsTagLimitExceeded`, `_FillingExactlyToMax_Succeeds`; `AlertsControllerTests.AddTags_WhenTagLimitExceeded_ReturnsValidationProblem` |
| AC5 | `AddTagsRequest` validation (1–30) | `AddTagsRequest_WithTagTrimmingToEmpty_FailsValidation`, `_WithTagLongerThan30Chars_FailsValidation`, `_WithValidTags_PassesValidation` |
| AC6 | `RemoveTagAsync`, repository remove | `AlertsControllerTests.RemoveTag_WhenRemoved_ReturnsNoContent`; `RemoveTagAsync_WhenTagAssigned_TrimsAndReturnsTrue`; `AlertRepositoryTests.RemoveTagAsync_WhenAssigned_RemovesAndReturnsTrue`, `_IsCaseInsensitive` |
| AC7 | `RemoveTagAsync` not-found paths, controller 404 | `AlertsControllerTests.RemoveTag_WhenNotFound_ReturnsNotFound`; `RemoveTagAsync_WhenAlertMissing_ReturnsFalse`, `_WhenTagNotAssigned_ReturnsFalse`; `AlertRepositoryTests.RemoveTagAsync_WhenNotAssigned_ReturnsFalse` |
| AC8 | `GetAllAsync` tag filter | `AlertRepositoryTests.GetAllAsync_WithTag_ReturnsOnlyAlertsCarryingTag`, `_WithTagAndOtherFilters_ReturnsOnlyAlertsMatchingAll`; `AlertManagementServiceTests.GetAllAsync_PassesQueryOptionsToRepository`; `AlertsControllerTests.AlertQueryRequest_WithTagLongerThan30Chars_FailsValidation` |
| AC9 | `AlertResponse.Tags`, mapping | `AlertRepositoryTests.GetByIdAsync_IncludesTags`; service add tests assert returned `Alert.Tags` |

## Test / Build Results (already recorded — not re-run)
- Build: production projects succeeded (0 warnings / 0 errors); both test projects compiled.
- `dotnet test AlertService.API.Tests` → **60 passed / 0 failed**.
- `dotnet test AlertService.Data.SQL.Tests` → **33 passed / 0 failed**.
- Total: **93 passing / 0 failing**. All ACs MET. Coverage tooling: `NOT_CONFIGURED`.

## Configuration / Database / Migration Impacts
- New EF Core migration `20261005181531_AddAlertTags` (Tag table + AlertTag join, unique CI index on `Tag.Name`).
- `database/02_AlertServiceDb_Migrations.sql` regenerated to stay consistent with the migration.
- Apply the migration (or run the idempotent SQL script) on deploy.

## Known Risks / Limitations
- Case-insensitive **tag filter** (AC8) and **global-tag reuse** rely on SQL Server's default CI collation. The EF InMemory/SQLite test providers compare ordinally, so those CI paths are unit-verified only at exact case; a reviewer or integration test may confirm against real SQL Server. Case-insensitive tag **removal** (AC6) is fully verified (service/repository compare in memory with `OrdinalIgnoreCase`).
- Four design defaults were implemented on their recommended settings and remain for reviewer confirmation (see `work.json` → `unresolved_questions`): tag normalization/casing, global vs per-alert tag table, POST response shape, filter/delete case-insensitivity.
- Out of scope: tag rename/merge, tag listing/autocomplete, UI, and auth/permission changes.
