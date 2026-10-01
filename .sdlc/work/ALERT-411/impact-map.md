# Impact Map: ALERT-411

## Direct Impact (see implementation-cache.json for the exact file list)
- `AlertService.API` — new `Options/AlertSuppressionOptions.cs`, `Program.cs` DI registration,
  `appsettings.json` new config section, `IAlertService`/`AlertManagementService` contract and
  logic change, `AlertsController` response-branching change.
- `AlertService.Common` — new header-name constant in `AlertConstants.cs`.
- `AlertService.Data` / `AlertService.Data.SQL` — new repository contract method +
  EF Core implementation (read-only query, no schema/migration change).

## Adjacent/Out-of-Scope (not touched)
- `AlertService.Models/Alert.cs`, `AlertDbContext`, EF migrations — no entity/schema changes;
  this story only adds a read query over existing columns.
- `AlertService.API/Extensions/HealthCheckEndpointExtensions.cs`, `Middleware/` — unaffected.
- Existing `Alert` CRUD endpoints (`GetById`, `Update`, `Deactivate`, `Delete`, tag endpoints) —
  behavior unchanged.
- `GET /api/alerts` listing/filtering — unchanged; duplicate suppression only affects `POST`.

## Downstream Consumers
- Any client calling `POST /api/alerts` may now receive `200 OK` (with the pre-existing alert
  and the new `X-Duplicate-Suppressed: true` header) instead of `201 Created` for near-duplicate
  submissions within the configured window. This is a behavior change clients should be aware of
  but is additive at the contract level (status code branch + new optional header; the response
  body shape is unchanged `AlertResponse`).
