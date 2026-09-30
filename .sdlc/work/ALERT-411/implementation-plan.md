# ALERT-411 Implementation Plan

## Plan
1. Add suppression-window configuration binding in API startup and flow into alert creation behavior.
2. Extend create-path logic in service layer to detect duplicates using configured window and required match criteria.
3. Update `POST /api/alerts` controller response branching to emit `200 OK` + `X-Duplicate-Suppressed: true` for suppressed duplicates, while retaining `201 Created` for new alerts.
4. Validate behavior with focused integration/service tests listed in `implementation-cache.json`.

## Notes
- Preserve existing controller -> service -> repository layering and existing response contracts except where story explicitly changes status/header behavior.