# Changes for ALERT-410 - Alert Tagging

Files added/modified:

- AlertService.Models/Tag.cs - new Tag domain model
- AlertService.Models/Alert.cs - added `Tags` navigation property
- AlertService.Data.SQL/AlertDbContext.cs - exposed `DbSet<Tag>`
- AlertService.Data.SQL/Configurations/AlertConfiguration.cs - updated to configure many-to-many
- AlertService.Data.SQL/Configurations/TagConfiguration.cs - new Tag configuration with unique normalized index
- AlertService.Data.SQL/Repositories/AlertRepository.cs - include tags in queries, filtering by tag, AddTagsAsync, RemoveTagAsync
- AlertService.Data/Interfaces/IAlertRepository.cs - signature updates for tag methods and tag filter
- AlertService.API/Controllers/AlertsController.cs - added endpoints to add/remove tags
- AlertService.API/Services/IAlertService.cs - added service methods for tags
- AlertService.API/Services/AlertManagementService.cs - implemented AddTagsAsync and RemoveTagAsync; pass tag filter
- AlertService.API/Mappings/AlertMappingExtensions.cs - included mapping to return tag names ordered case-insensitively
- AlertService.DTO/Responses/AlertResponse.cs - added `Tags` list
- AlertService.DTO/Requests/AlertTagRequest.cs - new request DTO with validation
- AlertService.Common/Constants/AlertConstants.cs - added tag constants (TagMaxLength, MaxTagsPerAlert)
- .sdlc/work/ALERT-410/session.md - updated status and summary
- .sdlc/work/ALERT-410/implementation-cache.json - (cache retained)

Notes:
- Tag rows are reused by `NormalizedName` (lowercase) to ensure global sharing.
- Per-alert tag limit enforced at repository level; input validation prevents most invalid payloads.
- `ToResponseWithTags()` returns tag names sorted case-insensitively while preserving original casing.

Validation:
- `dotnet build AlertService.sln` succeeded.
- Focused tests run:
  - `dotnet test AlertService.API.Tests --filter FullyQualifiedName~Alert` passed.
  - `dotnet test AlertService.Data.SQL.Tests --filter FullyQualifiedName~Alert` passed.

If you'd like, I can run the full test suite or create a migration script + SQL migration file next.
