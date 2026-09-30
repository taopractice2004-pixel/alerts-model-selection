Story: ALERT-411 — Duplicate Alert Suppression Window

Summary
- Operators are being flooded by near-duplicate alerts. The API must suppress creation of a new alert when an active alert with the same Title (case-insensitive) and Severity was already created within a configurable suppression window (minutes).

Acceptance (condensed)
- On `POST /api/alerts`, if an active alert with same Title (case-insensitive) and Severity exists and was created within the suppression window, do not create a new row. Return `200 OK` with the existing alert and header `X-Duplicate-Suppressed: true`.
- A genuine new alert must still return `201 Created`.
- Suppression window must be configurable from `appsettings.json` (not hardcoded).
- Do not suppress across different severities or if matched prior alert is inactive.

Scope anchors (smallest useful set)
- Controller: `AlertsController.Create` (POST /api/alerts)
- Service: `AlertManagementService.CreateAsync`
- Persistence boundary: `IAlertRepository.AddAsync` / `AlertRepository.AddAsync`

Constraints & notes
- Keep changes minimal and consistent with existing patterns (DI via Program.cs, options binding preferred).
- Unit tests touching `CreateAsync` and controller behavior will need updates or additions to verify suppression and response header.
