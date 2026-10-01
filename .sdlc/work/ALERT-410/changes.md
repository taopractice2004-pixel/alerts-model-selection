# Changes: ALERT-410 — Alert tagging

## Files Created

- `AlertService.Models/Tag.cs` — shared/global `Tag` entity (`Id`, `Name`, `AlertTags` nav).
- `AlertService.Models/AlertTag.cs` — explicit join entity (`AlertId`, `TagId`, `Alert`, `Tag`
  navs), matching the repo's existing explicit-configuration-class pattern instead of EF Core's
  implicit skip-navigation join table.
- `AlertService.Data.SQL/Configurations/TagConfiguration.cs` — `Tags` table, `Name` max length
  30, unique index on `Name` (relies on SQL Server's default case-insensitive collation — see
  Decisions below).
- `AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs` — `AlertTags` table, composite
  PK `(AlertId, TagId)`, cascade-delete FKs to `Alerts`/`Tags`, index on `TagId`.
- `AlertService.Data.SQL/Migrations/20261001121642_AddAlertTags.cs` (+ Designer, + updated
  `AlertDbContextModelSnapshot.cs`) — generated via
  `dotnet ef migrations add AddAlertTags --project AlertService.Data.SQL --startup-project AlertService.API`.
- `AlertService.DTO/Requests/AddAlertTagsRequest.cs` — `List<string> Tags`
  (`[Required] [MinLength(1)]`), plus `IValidatableObject.Validate` enforcing each tag is 1-30
  chars after trim (DataAnnotations-style, consistent with `AlertQueryRequest`'s existing
  `IValidatableObject` pattern — a plain `[StringLength]` cannot be applied per-list-item).

## Files Modified

- `AlertService.Models/Alert.cs` — added `List<AlertTag> Tags` navigation.
- `AlertService.Common/Constants/AlertConstants.cs` — added `TagMinLength = 1`,
  `TagMaxLength = 30`, `MaxTagsPerAlert = 10`.
- `AlertService.Data.SQL/AlertDbContext.cs` — added `DbSet<Tag> Tags`, `DbSet<AlertTag> AlertTags`.
- `AlertService.Data/Interfaces/IAlertRepository.cs` — added `tag` filter parameter to
  `GetAllAsync`; added `AddTagsAsync(int alertId, IReadOnlyCollection<string> tagNames, ...)`
  and `RemoveTagAsync(int alertId, string tagName, ...)`.
- `AlertService.Data.SQL/Repositories/AlertRepository.cs` —
  - `GetAllAsync`/`GetByIdAsync` now `Include(a => a.Tags).ThenInclude(at => at.Tag)` and apply
    an optional case-insensitive tag-name filter (`a.Tags.Any(at => at.Tag.Name.ToLower() == normalizedTag)`),
    ANDed with the existing filters (same pattern as the existing `search` filter's `.ToLower()`
    usage).
  - `AddTagsAsync` reuses an existing `Tag` row via case-insensitive name match or creates a new
    global row; skips names already assigned to the alert (case-insensitive); returns `null` if
    the alert does not exist.
  - `RemoveTagAsync` matches `{tag}` case-insensitively against the alert's assigned tags;
    returns `false` if the alert does not exist or has no such tag assigned.
- `AlertService.DTO/Requests/AlertQueryRequest.cs` — added optional `Tag` property
  (`[StringLength(TagMaxLength, MinimumLength = TagMinLength)]`).
- `AlertService.DTO/Responses/AlertResponse.cs` — added `IReadOnlyList<string> Tags` (names
  only, to avoid leaking internal `Tag` ids — consistent with other response DTOs).
- `AlertService.API/Mappings/AlertMappingExtensions.cs` — `ToResponse` now maps
  `alert.Tags.Select(at => at.Tag.Name)`.
- `AlertService.API/Services/IAlertService.cs` — added `AddTagsAsync` / `RemoveTagAsync`
  signatures; added `AddAlertTagsStatus` enum (`Success`, `AlertNotFound`, `TooManyTags`) and
  `AddAlertTagsResult` record (`Status`, `Alert`) in the same file, since no existing
  Result/outcome type exists in the repo and the business-rule failure (10-tag cap) is not
  expressible as a plain null/bool return like the other service methods.
- `AlertService.API/Services/AlertManagementService.cs` —
  - `GetAllAsync` now passes `request.Tag` through to the repository.
  - `AddTagsAsync`: normalizes/dedupes incoming tags case-insensitively (trim + ordinal-ignore-case
    distinct), computes the distinct tag count after the union with existing tags, and rejects
    the whole request (`TooManyTags`, nothing persisted) if it would exceed
    `AlertConstants.MaxTagsPerAlert`; otherwise delegates to the repository.
  - `RemoveTagAsync`: thin delegation to the repository with logging.
- `AlertService.API/Controllers/AlertsController.cs` —
  - `POST /api/alerts/{id}/tags` → `200 OK` with the updated `AlertResponse` on success,
    `404` if the alert doesn't exist, `400` (`ValidationProblemDetails`) if the cap would be
    exceeded.
  - `DELETE /api/alerts/{id}/tags/{tag}` → `204 No Content` on success, `404` if the alert or
    tag assignment doesn't exist (mirrors the existing `Delete` action's `bool`-returning
    pattern).
- `database/02_AlertServiceDb_Migrations.sql` — regenerated in full via
  `dotnet ef migrations script --idempotent --project AlertService.Data.SQL --startup-project AlertService.API --output database/02_AlertServiceDb_Migrations.sql`
  (see Decisions below). Diff is purely additive: the existing `InitialCreate` block is
  byte-for-byte unchanged; a new idempotent `AddAlertTags` transaction block was appended.
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` — updated two existing
  `IAlertRepository.GetAllAsync` mock setups/verifications to add the new `tag` positional
  parameter (`null`/matching value) so they keep compiling against the new interface signature.
  No new test scenarios were added here — adding tag-specific test coverage is left to the
  `/unit-testing` stage, per the separation of concerns between `/implement-story` and
  `/unit-testing` in this framework.

## Design Decisions Actually Implemented

All decisions match `implementation-cache.json`'s `design_decisions` as written:
- Global/shared `Tag` rows, explicit `AlertTag` join entity with composite PK.
- Case-insensitive dedup/lookup via `StringComparison.OrdinalIgnoreCase` (service layer) and
  `.ToLower()` comparisons (repository queries), consistent with the existing `search` filter.
- 10-tag cap enforced in `AlertManagementService` before persisting; request rejected (400)
  wholesale if exceeded — nothing partially applied.
- Each tag 1-30 chars after trim, validated via DTO-level `IValidatableObject` plus the
  cap/dedup check in the service layer.
- `DELETE .../tags/{tag}` returns 404 if the alert doesn't exist OR the alert has no such tag
  assigned (both cases collapse to the repository's `RemoveTagAsync` returning `false`).
- `Tag` filter on `GET /api/alerts` is ANDed with existing `isActive`/`severity`/date-range/search
  filters (added as one more `Where` clause in the same query pipeline).
- `AlertResponse.Tags` is `IReadOnlyList<string>` (names only).

No deviations from the cache's design decisions were required.

## Resolved `missing_facts` (from implementation-cache.json)

1. **`database/02_AlertServiceDb_Migrations.sql` regeneration**: Confirmed via
   `repo-profile.md` ("Operational assets: ... idempotent script generated from EF migrations")
   that this file is a generated artifact, not hand-maintained. Regenerated it with
   `dotnet ef migrations script --idempotent`, the standard EF Core tool for producing exactly
   this kind of idempotent, `__EFMigrationsHistory`-guarded script. The regenerated output's
   `InitialCreate` section is identical to the pre-existing file, confirming this is the correct
   generation command/tooling for this repo. Decision: **regenerate on every migration-adding
   story** (not NOT_CONFIGURED anymore — the command is now recorded here for reuse).
2. **Tag.Name case-insensitive uniqueness portability (SQL Server vs Sqlite/InMemory test
   providers)**: `TagConfiguration` declares a plain `builder.HasIndex(t => t.Name).IsUnique()`.
   SQL Server's default collation (`SQL_Latin1_General_CP1_CI_AS` or server/db default, which is
   case-insensitive in the vast majority of real deployments) makes this unique index
   case-insensitive in production without extra configuration. However:
   - The EF Core **InMemory** provider does not enforce unique indexes at all (by design), and
   - The **Sqlite** provider's default text collation is case-sensitive (`BINARY`), so a unique
     index there would **not** catch `"prod"` vs `"Prod"` as a duplicate.
   This is `TO_BE_DISCOVERED` / `NOT_CONFIGURED` beyond what was implemented: no
   project-specific convention for portable case-insensitive uniqueness (e.g. a computed
   normalized column, `COLLATE NOCASE` on Sqlite, or a value converter) was discoverable in the
   repo. Conservative, non-invented choice taken: rely on the SQL Server unique index for
   production correctness, and enforce case-insensitive de-duplication **in application code**
   (`AlertRepository.AddTagsAsync`'s case-insensitive lookup before insert) so correctness does
   not solely depend on the database collation — this keeps behavior correct across all three
   providers (SQL Server, Sqlite, InMemory) used in this repo's tests, at the cost of a
   theoretical race condition under concurrent inserts of the same new tag text on SQL Server
   (pre-existing concurrency-handling conventions were not found elsewhere in the repo to extend
   from, so none were added here). Flagged here rather than inventing a company-specific
   portable-collation rule.

## Validation Commands Run

| Command | Result |
|---|---|
| `dotnet tool restore` | Passed (`dotnet-ef` 8.0.31 already available) |
| `dotnet restore` | Passed (no-op, already restored) |
| `dotnet build AlertService.sln` | Passed — 0 warnings, 0 errors |
| `dotnet ef migrations add AddAlertTags --project AlertService.Data.SQL --startup-project AlertService.API` | Passed — migration `20261001121642_AddAlertTags` generated |
| `dotnet ef migrations script --idempotent --project AlertService.Data.SQL --startup-project AlertService.API --output database/02_AlertServiceDb_Migrations.sql` | Passed — script regenerated (not in the cache's `validation_commands` list verbatim, but matches the cache's own `migration_script_refresh` entry) |
| `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj` | Passed — 37/37 |
| `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj` | Passed — 25/25 |
| `dotnet test` (full solution) | Passed — 62/62 total |

## Flags

- `NOT_CONFIGURED`: portable (cross-provider) case-insensitive uniqueness mechanism for
  `Tag.Name` beyond SQL Server's default collation + application-level case-insensitive lookup
  (see Decisions above). No existing repo convention for this was discoverable.
- No other `NOT_AVAILABLE` / `TO_BE_DISCOVERED` gaps were encountered; `lint/format` tooling
  remains `NOT_CONFIGURED` per `repo-profile.md` (unchanged by this story).

## Cache Corrections

None required — `implementation-cache.json`'s `exact_source_files`, `exact_test_files`, and
`design_decisions` matched the actual touched slice. One test file
(`AlertManagementServiceTests.cs`, already listed in `exact_test_files`) required a minimal
signature-compatibility fix (not new test scenarios) due to the new `tag` parameter on
`IAlertRepository.GetAllAsync`.
