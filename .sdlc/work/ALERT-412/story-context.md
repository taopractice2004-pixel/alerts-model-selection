# ALERT-412 Story Context

## Story
- ID: ALERT-412
- Project: ALERT
- Title: Alert Volume Trend Endpoint
- Goal: Provide daily alert-creation trend data for dashboard charting with per-severity breakdowns.

## Required Behavior
- Expose `GET /api/alerts/trends?days=N`.
- `days` defaults to `7` and is constrained to `1..90`.
- Response returns one UTC calendar-day bucket per day for the last `N` days, ordered oldest to newest.
- Each bucket includes `TotalCount` and per-severity counts, including zeros for missing days/severities.
- Invalid `days` values (out of range or non-numeric) return `400` with existing `ValidationProblemDetails` behavior.

## Scope Ownership
- Exact implementation inventory, commands, and standards references are authoritative in `implementation-cache.json`.
