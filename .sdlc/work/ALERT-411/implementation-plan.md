# Implementation Plan - ALERT-411

## Change Strategy
- Introduce a create-alert outcome contract that lets the service report whether the returned alert was newly created or duplicate-suppressed.
- Add configurable suppression-window options sourced from `appsettings.json` and wired through the existing API startup path.
- Extend the repository boundary with a focused duplicate lookup by normalized title, severity, active status, and created-since cutoff.
- Update the create flow so the controller returns `200 OK` plus `X-Duplicate-Suppressed: true` when the service reports suppression, while preserving `201 Created` for new alerts.

## Validation Strategy
- Run focused API and SQL test projects first because they cover the create path controller, duplicate-suppression service behavior, and repository query logic, then run a solution build to confirm contract alignment.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Keep the first implementation slice centered on the existing create-alert endpoint rather than broad alert lifecycle changes.