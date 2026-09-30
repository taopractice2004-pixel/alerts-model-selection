# Impact Map - ALERT-412

## Primary Slice
- `/api/alerts/trends` through alert DTO, controller, service, repository abstraction, and SQL/EF aggregation.

## Adjacent Dependencies
- Existing `Severity` enum and summary response ordering conventions.
- Existing API model validation and `TimeProvider` composition.
- Existing controller, service, and repository test fixtures.

## Out Of Scope
- No schema or migration change is currently expected.
- No frontend/dashboard implementation, OpenAPI artifact, deployment change, or unrelated alert behavior.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
