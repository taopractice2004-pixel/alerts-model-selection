# Impact Map - ALERT-411

## Primary Slice
- `AlertService.API\Controllers\AlertsController.cs`
- `AlertService.API\Services\AlertManagementService.cs`
- `AlertService.Data.SQL\Repositories\AlertRepository.cs`

## Adjacent Dependencies
- `AlertService.API\Services\IAlertService.cs`
- `AlertService.Data\Interfaces\IAlertRepository.cs`
- `AlertService.API\appsettings.json`
- `AlertService.API\Program.cs`
- `AlertService.API\Mappings\AlertMappingExtensions.cs`
- `AlertService.Models\Alert.cs`
- `AlertService.DTO\Responses\AlertResponse.cs`

## Out Of Scope
- Non-create endpoints (`GET`, `PUT`, tag operations, deactivate, delete) except compile-time ripple from create-contract changes.
- Alert schema redesign, migrations, summary logic, and unrelated alert search/filter behavior.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
