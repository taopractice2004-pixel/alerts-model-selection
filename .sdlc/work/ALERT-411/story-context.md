# Story Context - ALERT-411

**Summary:** Suppress near-duplicate active alerts during a configurable time window.

**Business rule:** On `POST /api/alerts`, match active alerts by case-insensitive `Title`, exact `Severity`, and `CreatedDate` within the configured lookback. Return the existing alert with `200 OK` and `X-Duplicate-Suppressed: true`; otherwise create and return `201 Created`.

**Boundary:** Preserve controller -> service -> repository layering. Inactive alerts, different severities, and alerts outside the window must not suppress creation.

See `implementation-cache.json` for authoritative files, commands, and constraints.
