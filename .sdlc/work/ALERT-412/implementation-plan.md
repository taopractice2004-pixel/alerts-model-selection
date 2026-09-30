# Implementation Plan - ALERT-412

## Plan
1. Add a dedicated query DTO for `days` with default/range validation so invalid values flow through the existing `ApiController` model-validation path.
2. Add dedicated trend response DTOs for a multi-day payload and daily buckets.
3. Extend the service contract and service implementation with a trend retrieval method that preserves the existing severity ordering convention.
4. Extend the repository contract and SQL implementation to aggregate alert counts by UTC date and severity for the last `N` days, then fill missing days and severities with zeros.
5. Add focused controller, service, and repository tests for validation, ordering, zero-fill behavior, and UTC day bucketing.
6. Run focused tests, then build the solution.

## Acceptance Mapping
- Route and query validation: steps 1, 3, 5
- Oldest-first UTC day buckets: steps 3, 4, 5
- Total and per-severity counts with zero-fill: steps 2, 3, 4, 5
- Existing severity ordering: steps 3, 5