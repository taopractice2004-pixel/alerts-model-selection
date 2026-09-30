# Impact Map — ALERT-410

## Primary Slice
- Alert tagging feature across layers: `Tag`/`AlertTag` model, `AlertDbContext` + EF config +
  migration, `IAlertRepository`/`AlertRepository`, `IAlertService`/`AlertManagementService`,
  `AlertsController`, request/response DTOs, and `AlertMappingExtensions`.

## Implementation Note
- The explicit join entity lives in `AlertService.Models/AlertTag.cs` and its EF mapping in a
  dedicated `AlertService.Data.SQL/Configurations/AlertTagConfiguration.cs` (rather than folding
  the join config into `AlertConfiguration.cs`). `AlertConfiguration.cs` was left unchanged.

## Adjacent Dependencies
- `AlertService.Data/Interfaces/IAlertRepository.cs` (contract change consumed by service layer).
- `AlertService.API/Services/IAlertService.cs` (contract change consumed by controller).
- `AlertService.Common/Constants/AlertConstants.cs` (shared tag constants).
- `database/02_AlertServiceDb_Migrations.sql` (idempotent SQL mirror of the EF migration).

## Out Of Scope
- No changes to summary endpoint, health checks, paging/sorting semantics, or existing filters
  beyond adding the composable `tag` filter.
- No new frontend/UI (solution is backend-only).
- No auth/authorization model changes.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
