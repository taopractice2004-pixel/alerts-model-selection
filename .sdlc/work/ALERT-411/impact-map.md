# Impact Map — ALERT-411

## Primary Slice
- Alert create path for `POST /api/alerts` in controller/service/repository.
- Alert duplicate suppression configuration binding (`AlertDuplicateSuppression:WindowMinutes`) and create-result shaping in service contract.

## Adjacent Dependencies
- API response contract behavior in `AlertsController.Create` (status code and header behavior).
- Config binding surface from `AlertService.API/appsettings.json` (duplicate suppression window).
- Repository query semantics and indexing/perf expectations for title+severity+active+createdDate filtering.

## Out Of Scope
- Changes to alert query/filtering endpoints.
- Alert update/deactivate/delete/tag behavior.
- Cross-service/global deduplication mechanisms beyond this API create path.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.
