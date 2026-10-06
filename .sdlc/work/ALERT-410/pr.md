# PR Draft — ALERT-410

## Title
ALERT-410: Add alert tagging (add/remove tags, tag filter, tags in AlertResponse)

## Description
Operators can attach and remove free-form tags on alerts, filter `GET /api/alerts` by tag, and see tags on every `AlertResponse`. Adds a `Tag` entity with a many-to-many join to `Alert` and an EF migration.

## Story / Requirement Summary
- Source: `work.json` → `summary`, AC1–AC8.
- `POST /api/alerts/{id}/tags` (trim, 1–30 chars, case-insensitive dedupe, max 10 per alert, 404/400).
- `DELETE /api/alerts/{id}/tags/{tag}` (204, 404 for missing alert or unassigned tag).
- `GET /api/alerts?tag=` composes (AND) with existing filters, paging, sorting, `TotalCount`.
- `AlertResponse.Tags` on list, get by id, create, update, deactivate, add-tags.

## Implementation Summary
- `Tag` entity (`Name`, unique `NormalizedName`) and `Alert.Tags` many-to-many via `AlertTags` join table (cascade delete); `TagConfiguration`; migration `AddAlertTags`; `database/02_AlertServiceDb_Migrations.sql` regenerated.
- Repository: tag filter, `Include(Tags)` on reads, `AddTagsAsync`, `RemoveTagAsync`; `UpdateAsync` marks only the alert as modified so loaded tags are not rewritten.
- Service: owns tag rules and returns `AddAlertTagsResult`; controller exposes the two new endpoints and the `tag` query parameter.
- DTOs: `AddAlertTagsRequest`, `AlertQueryRequest.Tag`, `AlertResponse.Tags`; mapping updated in `AlertMappingExtensions.ToResponse`.

## Changed Files
Source: read-only `git status --porcelain` (22 source/test files; `.github/`, `.sdlc/`, `standards/` are untracked framework files, not part of this change).

Modified:
- AlertService.API/Controllers/AlertsController.cs
- AlertService.API/Mappings/AlertMappingExtensions.cs
- AlertService.API/Services/AlertManagementService.cs
- AlertService.API/Services/IAlertService.cs
- AlertService.Common/Constants/AlertConstants.cs
- AlertService.DTO/Requests/AlertQueryRequest.cs
- AlertService.DTO/Responses/AlertResponse.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/AlertDbContext.cs
- AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.Models/Alert.cs
- database/02_AlertServiceDb_Migrations.sql

Added:
- AlertService.API/Services/AddAlertTagsResult.cs
- AlertService.DTO/Requests/AddAlertTagsRequest.cs
- AlertService.Data.SQL/Configurations/TagConfiguration.cs
- AlertService.Data.SQL/Migrations/20261005180228_AddAlertTags.cs
- AlertService.Data.SQL/Migrations/20261005180228_AddAlertTags.Designer.cs
- AlertService.Models/Tag.cs

Tests (modified):
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

## Acceptance Criteria Traceability
| AC | Implemented in | Proven by |
|---|---|---|
| AC1 | `Tag`, `TagConfiguration`, `AlertDbContext`, `AddAlertTags` migration, `database/02_*.sql` | `AlertRepositoryTests` (add/remove, tag reuse, Sqlite unique index + cascade delete) |
| AC2 | `AlertsController.AddTags`, `AlertManagementService.AddTagsAsync` | Service + controller tests (200, 404) |
| AC3 | `AlertManagementService.AddTagsAsync` (trim, case-insensitive dedupe) | Service tests (dedupe, existing-tag skip, idempotency); repository tests |
| AC4 | `AlertManagementService.AddTagsAsync`, `AddAlertTagsRequest` | Service + controller tests (length 1–30, blank, 10-tag limit, 400, nothing persisted) |
| AC5 | `AlertsController.RemoveTag`, `AlertManagementService.RemoveTagAsync` | Service + controller tests (204, 404) ; repository remove tests |
| AC6 | `AlertQueryRequest.Tag`, `AlertRepository.GetAllAsync` | Controller pass-through + repository filter tests (incl. Sqlite case-insensitive) |
| AC7 | `AlertRepository.GetAllAsync` | Repository tests (compose with other filters, paging, sorting, `TotalCount`, no duplicate rows) |
| AC8 | `AlertResponse.Tags`, `AlertMappingExtensions.ToResponse` | Service tests (tags in all responses); repository `Include` tests |

## Test / Build Results (already recorded, not re-run)
- Build: `dotnet build AlertService.API/AlertService.API.csproj` succeeded (0 warnings, 0 errors).
- `dotnet test AlertService.Data.SQL.Tests` — 44 passed.
- `dotnet test AlertService.API.Tests` — 75 passed.
- Coverage: `AlertManagementService`, `AlertsController`, `AlertMappingExtensions`, `AddAlertTagsResult` 100% line/branch; `AlertService.Data.SQL` coverage `NOT_CONFIGURED`.
- Test → fix loop: 0/3 — `TESTS_PASSED`; all ACs MET; no open bugs.

## Configuration / Database / Migration Impacts
- New EF migration `AddAlertTags` (new `Tags` and `AlertTags` tables, unique index on `Tags.NormalizedName`); apply via `database/02_AlertServiceDb_Migrations.sql` or `dotnet ef database update`.
- No configuration changes; no new dependencies.

## Known Risks / Limitations
- Assumptions pending human confirmation: POST returns 200 with the updated `AlertResponse`; exceeding 10 tags returns 400; DELETE returns 204; POST is idempotent (200) when all tags already exist.
- Orphan `Tag` rows are kept when the last assignment is removed (cleanup deferred).
- Case-insensitive matching is done via a normalized key rather than DB collation; first-seen casing is displayed.
