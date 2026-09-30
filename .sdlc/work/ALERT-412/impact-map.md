# Impact Map — ALERT-412

## In Scope (see `implementation-cache.json` for exact file list)
- `AlertsController`: new `GET /api/alerts/trends` action
- `IAlertService` / `AlertManagementService`: new `GetTrendsAsync`, bucket zero-fill logic
- `IAlertRepository` / `AlertRepository`: new query for per-day, per-severity creation counts
  over a UTC date range
- New DTOs: trends query request (`Days` with `[Range(1,90)]`, default 7) and trends response
  (per-day bucket reusing `AlertSeverityCountsResponse`)

## Adjacent, Not Modified
- `AlertSeverityCountsResponse` (reused as-is for per-bucket severity counts)
- `AlertConstants` (extended with new min/max/default day constants, not restructured)
- `TimeProvider` singleton registration in `Program.cs` (reused, not changed)

## Out Of Scope
- No changes to `GET /api/alerts/summary` behavior
- No changes to alert creation, update, deactivation, deletion, or tagging flows
- No new migrations or schema changes (query aggregates existing `Alerts.CreatedDate` /
  `Alerts.Severity` columns)
