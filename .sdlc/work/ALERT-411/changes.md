# Changes - ALERT-411

- Added configurable `Alerts:DuplicateSuppressionWindowMinutes` (5 minutes).
- Added repository lookup for newest active near-duplicate alerts using case-insensitive title, exact severity, and UTC lookback filtering.
- Updated service creation to suppress duplicates without inserting a new alert.
- Updated POST response behavior: suppressed alerts return `200 OK` with `X-Duplicate-Suppressed: true`; new alerts retain `201 Created`.
- Added focused controller, service, and repository tests.

Validation: all required build and focused test commands passed on 2026-09-30.
