# Impact Map - ALERT-412

## Primary Impact
- API surface: add `GET /api/alerts/trends` plus dedicated request/response DTOs.
- Service layer: add a trend retrieval contract and response mapping path aligned with summary severity ordering.
- Data layer: add repository aggregation for UTC daily counts by severity.

## Adjacent Dependencies
- Existing query-validation behavior under `[ApiController]` for `400` `ValidationProblemDetails` responses.
- Existing `AlertSeverityCountsResponse` and `Severity` enum ordering conventions.
- Existing repository test coverage for aggregate queries and date filtering patterns.

## Out Of Scope
- Changes to the existing list or summary endpoint contracts.
- Database schema or migration changes.
- Non-UTC bucketing or caller-specified timezones.