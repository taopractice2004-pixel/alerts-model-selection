# Impact Map — ALERT-410

## Primary Slice
- `AlertService.API/Controllers/AlertsController.cs` (new tag routes + `tag` query param)
- `AlertService.API/Services/AlertManagementService.cs` / `IAlertService.cs` (add/remove-tag methods)
- `AlertService.Models/Alert.cs` + new `AlertService.Models/Tag.cs` (many-to-many nav)

## Adjacent Dependencies
- `AlertService.Data/Interfaces/IAlertRepository.cs` (tag filter param, tag mutation methods)
- `AlertService.Data.SQL/AlertDbContext.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`,
  new EF configuration + migration under `AlertService.Data.SQL/Migrations`
- `AlertService.DTO/Responses/AlertResponse.cs`, `AlertService.DTO/Requests/AlertQueryRequest.cs`,
  new tag-add request DTO
- `AlertService.API/Mappings/AlertMappingExtensions.cs`
- `AlertService.Common/Constants/AlertConstants.cs` (new tag limit constants)

## Out Of Scope
- DI registration (`AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs`) — unaffected,
  existing `AddScoped<IAlertRepository, AlertRepository>()` covers the extended interface.
- Custom exception-to-4xx middleware convention — not introduced for this story; the max-10-tags
  business rule will be surfaced without a new global convention (implementation-time decision).

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
