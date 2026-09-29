# 202 Implementation Plan

1. Add a bulk acknowledgement request DTO and response DTO.
2. Add one new alerts controller action for bulk acknowledgement.
3. Extend IAlertService and AlertManagementService with one bulk acknowledgement operation.
4. Extend repository abstractions with the minimum bulk retrieval and persistence support needed by the service.
5. If acknowledgement is confirmed as persisted state, add acknowledgement fields to Alert and create the required EF migration and SQL artifact update.
6. Add mirrored controller, service, and repository tests for success, partial success, and no-op or ineligible paths.

## Service Rule Placement
- Controller: transport only
- Service: eligibility and operation result logic
- Repository: data access and batch persistence only

## Preferred Conservative Endpoint Shape
- Route assumption: PATCH /api/alerts/acknowledge
- Request assumption: collection of alert ids
- Response assumption: requested count plus acknowledged, ineligible, and not-found results

## Validation
See implementation-cache.json for the authoritative validation and reproduction commands.

## Blockers
Clarify acknowledgement semantics, eligibility, and acceptance criterion 4 intent before implementation if possible.