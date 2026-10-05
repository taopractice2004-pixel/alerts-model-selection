# PR Draft — ALERT-410

## Title
ALERT-410: Add alert tagging (add/remove tags, tag filter, tags in response)

## Description
Operators can attach free-form tags to alerts, remove them, filter `GET /api/alerts` by tag, and see tags in every `AlertResponse`. Adds a `Tag` entity, `AlertTags` join table and an EF migration.

## Story / Requirement Summary
"As an operator, I want to attach multiple free-form tags to an alert so I can filter alerts by tag." Acceptance criteria AC1–AC9 are tracked in `work.json`.

## Implementation Summary
- `Tag` entity with many-to-many to `Alert` (`AlertTags`, cascade on join rows, unique `IX_Tags_Name`); new migration `AddAlertTags`; `database/02_AlertServiceDb_Migrations.sql` regenerated.
- `POST /api/alerts/{id}/tags` (200 + updated alert; 404 unknown alert; 400 invalid/over-limit) and `DELETE /api/alerts/{id}/tags/{tag}` (204; 404 unknown alert or assignment).
- Tag rules live in `AlertManagementService`: trim, case-insensitive dedupe (first-seen casing kept), idempotent re-add, max 10 per alert (rejected whole). Limits are constants in `AlertConstants`.
- `GET /api/alerts` gains optional `tag` filter (case-insensitive, AND with existing filters, paging/sorting/TotalCount); tags are included in list and detail results.
- `AlertResponse.Tags` added (empty list when none, sorted).

## Changed Files (source: `git status --porcelain`, 22 files; unrelated `.github` / `.sdlc/context` / `.sdlc/templates` edits excluded)
New:
- AlertService.Models/Tag.cs
- AlertService.DTO/Requests/AddAlertTagsRequest.cs
- AlertService.Data.SQL/Configurations/TagConfiguration.cs
- AlertService.Data.SQL/Migrations/20261005151038_AddAlertTags.cs (+ `.Designer.cs`)
- AlertService.API/Services/AddAlertTagsResult.cs

Modified:
- AlertService.Models/Alert.cs
- AlertService.Common/Constants/AlertConstants.cs
- AlertService.DTO/Requests/AlertQueryRequest.cs, AlertService.DTO/Responses/AlertResponse.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/AlertDbContext.cs, Repositories/AlertRepository.cs, Migrations/AlertDbContextModelSnapshot.cs
- AlertService.API/Controllers/AlertsController.cs, Services/IAlertService.cs, Services/AlertManagementService.cs, Mappings/AlertMappingExtensions.cs
- database/02_AlertServiceDb_Migrations.sql
- Tests: AlertService.API.Tests/Controllers/AlertsControllerTests.cs, AlertService.API.Tests/Services/AlertManagementServiceTests.cs, AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

## Acceptance Criteria Traceability
| AC | Implemented in | Validated by |
|---|---|---|
| AC1 Tag entity, join table, migration | `Tag`, `TagConfiguration`, `AlertDbContext`, `AddAlertTags` migration, SQL script | `AlertRepositoryTests` (model, unique index, migration, cascade; SQLite) |
| AC2 POST adds tags, 404 | `AlertsController`, `AlertManagementService.AddTagsAsync` | `AlertsControllerTests`, `AlertManagementServiceTests` |
| AC3 Case-insensitive dedupe | `AlertManagementService` | `AlertManagementServiceTests` (request, existing, idempotent, trim) |
| AC4 Max 10 tags, 400 | `AlertManagementService`, `AlertConstants` | `AlertManagementServiceTests` (10 ok / 11 rejected), `AlertsControllerTests` (400) |
| AC5 Tag length 1–30 | `AddAlertTagsRequest` validation | `AlertsControllerTests` (empty/whitespace/null/31 fail; 1 and 30 pass) |
| AC6 DELETE 204/404 | `AlertsController`, `AlertManagementService.RemoveTagAsync`, `AlertRepository` | controller, service and repository tests |
| AC7 `tag` filter | `AlertQueryRequest`, `AlertRepository.GetAllAsync` | `AlertRepositoryTests` (case-insensitive, trimmed, unknown → empty) |
| AC8 Filter composes with others | `AlertRepository.GetAllAsync` | `AlertRepositoryTests` (isActive, severity, date range, search, paging/sorting/TotalCount; SQLite) |
| AC9 Tags in `AlertResponse` | `AlertResponse`, `AlertMappingExtensions` | service and repository tests (empty list, sorted, loaded by GetAll/GetById) |

## Test / Build Results (recorded, not re-run)
- `dotnet build AlertService.sln` — 0 warnings, 0 errors
- `dotnet test AlertService.API.Tests` — 70/70 passed
- `dotnet test AlertService.Data.SQL.Tests` — 49/49 passed
- Coverage (API tests): `AlertManagementService`, `AlertsController`, `AddAlertTagsRequest`, `AlertQueryRequest` 100% line/branch; `AlertMappingExtensions` 100% line / 50% branch (pre-existing branch); assembly-wide 48.9% line / 63.0% branch.

## Configuration / Database / Migration Impacts
- New migration `20261005151038_AddAlertTags`: creates `Tags` and `AlertTags` (join, cascade on alert delete) with unique `IX_Tags_Name`. Apply the regenerated script in `database/02_AlertServiceDb_Migrations.sql`.
- No configuration or dependency changes. API changes are additive (`tag` query parameter, `tags` response field, two new routes).

## Known Risks / Limitations
- Concurrent creation of the same new tag can hit the unique index and return 500.
- A tag containing `/` cannot be deleted through the route.
- HTTP-level model validation and routing are not covered by unit tests.
- No authentication/authorization on the new endpoints (matches existing endpoints; policy `TO_BE_DISCOVERED`).
- Seven assumptions in `work.json` → `unresolved_questions` are unconfirmed (POST returns 200; over-limit rejected whole; trim + first-seen casing; idempotent re-add and empty array → 400; shared tags without orphan cleanup; single-value filter; join rows cascade).
