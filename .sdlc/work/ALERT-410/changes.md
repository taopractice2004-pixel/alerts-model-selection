# Changes — ALERT-410

## Implemented Scope
- Added normalized many-to-many alert tagging (`Alert` <-> `Tag`) with EF configuration and migration:
  - New `Tag` entity and `Alert.Tags` navigation.
  - New `TagConfiguration` and updated `AlertConfiguration` join table mapping (`AlertTags`).
  - New EF migration `20261001140942_AddAlertTags` plus snapshot update.
- Added tagging APIs on alerts:
  - `POST /api/alerts/{id}/tags` to assign a tag.
  - `DELETE /api/alerts/{id}/tags/{tag}` to remove assignment.
- Added service-layer tag behavior:
  - Case-insensitive deduplication via normalized tag comparison.
  - Max 10 tags per alert enforcement.
  - Tag validation guard (non-empty, max length 30).
  - Explicit status handling for not-found/duplicate/not-assigned/max-reached outcomes.
- Added query and projection support:
  - Optional `Tag` query filter on `GET /api/alerts` composed with existing filters.
  - Included tags in `AlertResponse`.

## Files Added
- `AlertService.Models/Tag.cs`
- `AlertService.DTO/Requests/AddAlertTagRequest.cs`
- `AlertService.API/Services/AlertTagOperationResult.cs`
- `AlertService.Data.SQL/Configurations/TagConfiguration.cs`
- `AlertService.Data.SQL/Migrations/20261001140942_AddAlertTags.cs`
- `AlertService.Data.SQL/Migrations/20261001140942_AddAlertTags.Designer.cs`

## Files Updated
- `AlertService.API/Controllers/AlertsController.cs`
- `AlertService.API/Mappings/AlertMappingExtensions.cs`
- `AlertService.API/Services/AlertManagementService.cs`
- `AlertService.API/Services/IAlertService.cs`
- `AlertService.Common/Constants/AlertConstants.cs`
- `AlertService.DTO/Requests/AlertQueryRequest.cs`
- `AlertService.DTO/Responses/AlertResponse.cs`
- `AlertService.Data/Interfaces/IAlertRepository.cs`
- `AlertService.Data.SQL/AlertDbContext.cs`
- `AlertService.Data.SQL/Configurations/AlertConfiguration.cs`
- `AlertService.Data.SQL/Repositories/AlertRepository.cs`
- `AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs`
- `AlertService.Models/Alert.cs`
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`

## Validation Run
- `dotnet build AlertService.sln`
- `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"`
- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"`
