# Impact Map - ALERT-410

## Primary Slice
- `/api/alerts` contracts and `AlertsController`, `AlertManagementService`, alert repository query/persistence, and explicit alert mapping.

## Adjacent Dependencies
- `AlertService.Models`, `AlertService.DTO`, `AlertService.Data.SQL/AlertDbContext`, EF configurations, migrations, and existing API/service/repository tests.

## Out Of Scope
- No frontend/UI changes, deployment changes, unrelated alert behavior, or broad repository refactoring.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
