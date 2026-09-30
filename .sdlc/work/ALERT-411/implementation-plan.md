# Implementation Plan - ALERT-411

1. Add a configurable duplicate-suppression window under `Alerts` in `appsettings.json` and inject/read it through the existing service composition pattern.
2. Extend the repository contract and SQL implementation with an async lookup for the newest active alert matching case-insensitive title, exact severity, and the UTC lookback boundary.
3. Have the service return both the response and suppression state while preserving existing alert creation mapping and time-provider usage.
4. Update the POST controller to emit `200 OK` plus `X-Duplicate-Suppressed: true` for suppression, otherwise retain `201 Created`.
5. Add focused controller, service, and repository tests for positive suppression and each exclusion rule.

Validation commands are authoritative in `implementation-cache.json`.
