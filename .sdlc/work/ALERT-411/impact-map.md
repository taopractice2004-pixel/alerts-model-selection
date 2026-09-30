# ALERT-411 Impact Map

## In Scope
- `POST /api/alerts` status/header branching for duplicate suppression outcome.
- Service-layer duplicate detection criteria: title case-insensitive, same severity, active-only, created within configured window.
- App configuration for suppression window minutes.
- Integration/service tests covering suppression and non-suppression paths.

## Adjacent Dependencies
- Existing create-alert validation and mapping must remain intact.
- Existing integration test infrastructure already injects `AlertSuppression:DuplicateWindowMinutes` and should remain the primary test seam.

## Out of Scope
- New endpoints or route shape changes.
- Cross-story features (for example trends behavior).
- Broad refactors outside the alert create path and required configuration wiring.