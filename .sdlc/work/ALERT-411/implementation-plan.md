# Implementation Plan — ALERT-411

## Change Strategy
- Add a scoped duplicate-check path to alert creation flow (`AlertManagementService.CreateAsync`) that queries for an existing active alert matching normalized title + severity within a configurable window and returns a suppression result when matched.
- Extend repository contract/implementation with a single-purpose query for latest matching active alert within cutoff timestamp; keep comparison semantics explicit (case-insensitive title, same severity, active only).
- Update create endpoint response handling to emit `200 OK` + `X-Duplicate-Suppressed: true` for suppressed duplicates while preserving current `201 Created` response for new alerts.
- Introduce duplicate suppression window configuration in `appsettings.json` and wire it into service construction using existing DI/config patterns.

## Validation Strategy
- Run focused API/service/repository unit tests covering suppressed and non-suppressed create paths, including severity mismatch, inactive prior alert, and outside-window behavior; then run solution build for compile safety.

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- Keep changes minimal and local to create-alert flow; no behavior change for GET/PUT/PATCH/DELETE paths.
