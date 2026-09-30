# ALERT-411 Story Context

## Story
- ID: ALERT-411
- Project: ALERT
- Title: Duplicate Alert Suppression Window
- Goal: Prevent duplicate active alerts in a configurable recent time window while preserving normal create behavior for non-duplicates.

## Required Behavior
- On `POST /api/alerts`, detect duplicates by `Title` (case-insensitive) + `Severity` + `IsActive=true` + created within suppression window minutes.
- Duplicate path returns `200 OK` with the existing alert payload and response header `X-Duplicate-Suppressed: true`.
- Non-duplicate path keeps `201 Created` behavior.
- Suppression window minutes comes from app configuration (not hardcoded).
- Do not suppress across different severities.
- Do not suppress when prior matching alert is inactive.

## Scope Ownership
- Exact implementation inventory is authoritative in `implementation-cache.json`.