# Story Context — ALERT-412

**Title:** Alert Volume Trend Endpoint
**As a** dashboard consumer, **I want** daily alert-creation counts broken down by severity so I
can chart trends.

## Acceptance Criteria (summary)
- New `GET /api/alerts/trends?days=N` (default 7, min 1, max 90); one bucket per UTC calendar
  day for the last `N` days, oldest first; each bucket has a total count and a per-severity
  count, including zero-count days/severities.
- Reuses the existing severity ordering/shape convention from `GET /api/alerts/summary`
  (`AlertSeverityCountsResponse`: Low, Medium, High, Critical).
- Invalid `days` (out of range or non-numeric) returns `400` with `ValidationProblemDetails`,
  matching the existing `[Range]`-attribute + `[ApiController]` auto-validation convention used
  by `AlertQueryRequest`.

## Relationship To Repository
Standalone story on top of the existing layered architecture (see
`.sdlc/context/repo-profile.md`). Follows the same Controller → Service → `IAlertRepository` →
EF Core path already used by `GET /api/alerts/summary`.

## Notes
See `implementation-cache.json` for exact files, constraints, and standards. No unresolved
business questions — bucket zero-filling and severity ordering are fully specified by the
acceptance criteria and the existing summary-endpoint convention.
