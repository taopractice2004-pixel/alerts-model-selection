# Impact Map - ALERT-410

## Primary Slice
- `AlertService.API\Controllers\AlertsController.cs`
- `AlertService.API\Services\AlertManagementService.cs`
- `AlertService.Data.SQL\Repositories\AlertRepository.cs`

## Adjacent Dependencies
- `AlertService.API\Services\IAlertService.cs`
- `AlertService.API\Services\AlertTagOperationStatus.cs`
- `AlertService.API\Services\AlertTagOperationResult.cs`
- `AlertService.API\Mappings\AlertMappingExtensions.cs`
- `AlertService.DTO\Requests\AddAlertTagsRequest.cs`
- `AlertService.DTO\Requests\AlertQueryRequest.cs`
- `AlertService.DTO\Responses\AlertResponse.cs`
- `AlertService.Data\Interfaces\IAlertRepository.cs`
- `AlertService.Data.SQL\AlertDbContext.cs`
- `AlertService.Data.SQL\Configurations\AlertConfiguration.cs`
- `AlertService.Data.SQL\Configurations\TagConfiguration.cs`
- `AlertService.Data.SQL\Configurations\AlertTagConfiguration.cs`
- `AlertService.Models\Alert.cs`
- `AlertService.Models\Tag.cs`
- `AlertService.Models\AlertTag.cs`
- `AlertService.Common\Constants\AlertConstants.cs`

## Out Of Scope
- Standalone tag CRUD endpoints or non-alert tag reporting.
- DI/startup, health endpoints, logging pipeline, and unrelated alert summary behavior unless required by compile-time changes.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
