# ALERT-410 Changes

## Summary
- Added alert tagging support with many-to-many persistence (`Tag` + `AlertTag`).
- Added `POST /api/alerts/{id}/tags` and `DELETE /api/alerts/{id}/tags/{tag}` endpoints.
- Added optional `tag` filter to `GET /api/alerts` and projected tags in `AlertResponse`.
- Added EF migration `20260930071610_AddAlertTagging`.

## Source Changes
- API layer
  - `AlertService.API/Controllers/AlertsController.cs`
  - `AlertService.API/Services/IAlertService.cs`
  - `AlertService.API/Services/AlertManagementService.cs`
  - `AlertService.API/Mappings/AlertMappingExtensions.cs`
- Data abstractions and SQL implementation
  - `AlertService.Data/Interfaces/IAlertRepository.cs`
  - `AlertService.Data.SQL/AlertDbContext.cs`
  - `AlertService.Data.SQL/Repositories/AlertRepository.cs`
  - `AlertService.Data.SQL/Configurations/TagConfiguration.cs`
  - `AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs`
  - `AlertService.Data.SQL/Migrations/20260930071610_AddAlertTagging.cs`
  - `AlertService.Data.SQL/Migrations/20260930071610_AddAlertTagging.Designer.cs`
  - `AlertService.Data.SQL/Migrations/AlertDbContextModelSnapshot.cs`
- DTO/model/common
  - `AlertService.DTO/Requests/AddTagsRequest.cs`
  - `AlertService.DTO/Requests/AlertQueryRequest.cs`
  - `AlertService.DTO/Responses/AlertResponse.cs`
  - `AlertService.Models/Alert.cs`
  - `AlertService.Models/Tag.cs`
  - `AlertService.Models/AlertTag.cs`
  - `AlertService.Common/Constants/AlertConstants.cs`
- Auxiliary compile-contract fix for existing tests
  - `AlertService.DTO/Responses/AlertTrendResponse.cs`

## Test Changes
- `AlertService.API.Tests/Controllers/AlertsControllerTests.cs` (add/remove tag endpoint coverage)
- `AlertService.API.Tests/Services/AlertManagementServiceTests.cs` (tag limits/dedupe/remove coverage)
- `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs` (tag filter/add/remove coverage)

## Validation
- Passed
  - `dotnet tool restore`
  - `dotnet restore`
  - `dotnet build AlertService.sln`
  - `dotnet test AlertService.API.Tests --filter "FullyQualifiedName~TC_410_|FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertManagementServiceTests"`
  - `dotnet test AlertService.Data.SQL.Tests`
  - `dotnet ef migrations add AddAlertTagging --project AlertService.Data.SQL --startup-project AlertService.API`
- Not fully passing / blocked by out-of-scope pre-existing gaps
  - `dotnet test` fails ALERT-411/ALERT-412 integration scenarios (duplicate suppression + trends endpoints not present in current API source)
  - `dotnet ef database update --project AlertService.Data.SQL --startup-project AlertService.API` failed on local DB because object `Tags` already exists
