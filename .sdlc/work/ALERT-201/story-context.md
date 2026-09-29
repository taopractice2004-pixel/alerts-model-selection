# ALERT-201 Story Context

## Goal
Add optional created-date range filtering to GET /api/alerts so analysts can investigate alerts within a specific time window.

## In Scope
- Optional `createdFrom` and `createdTo` query parameters
- Inclusive lower-bound and upper-bound filtering on `CreatedDate`
- Request validation that rejects `createdFrom > createdTo` as `400 Bad Request`
- Preservation of existing active-status, severity, title-search, sorting, and paging behavior
- Focused unit tests for validation, service pass-through, and repository filtering

## Constraints
- No database schema or migration changes
- Keep the existing controller -> service -> repository layering intact
- Reuse existing `ApiController` model-validation behavior where the request contract can express the rule

## Standards
See `implementation-cache.json` for the authoritative standards list.
