# Impact Map - ALERT-410

## Primary Slice
- Alert list/query behavior plus tag assignment and tag removal within the existing alerts API/service/repository flow.

## Adjacent Dependencies
- AlertService.Common/Constants/AlertConstants.cs
- AlertService.DTO/Requests/AlertQueryRequest.cs
- AlertService.DTO/Requests/AssignAlertTagsRequest.cs
- AlertService.DTO/Responses/AlertResponse.cs
- AlertService.API/Mappings/AlertMappingExtensions.cs
- AlertService.API/Middleware/ExceptionHandlingMiddleware.cs
- AlertService.Data/Interfaces/IAlertRepository.cs
- AlertService.Data.SQL/Repositories/AlertRepository.cs
- AlertService.Models/Alert.cs
- AlertService.Models/Tag.cs
- AlertService.Data.SQL/AlertDbContext.cs
- AlertService.Data.SQL/Configurations
- AlertService.Data.SQL/Migrations
- database/02_AlertServiceDb_Migrations.sql

## Out Of Scope
- Standalone tag CRUD endpoints.
- UI or client-side filtering changes.
- Authorization or identity changes.
- Broad alert filtering redesign unrelated to tag composition.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
