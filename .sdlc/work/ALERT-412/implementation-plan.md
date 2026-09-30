# ALERT-412 Implementation Plan

## Plan
1. Add trends query binding and endpoint handler for `GET /api/alerts/trends` with model-validation-compatible `days` bounds/default behavior.
2. Extend service contract/implementation to request aggregated counts and materialize complete UTC day buckets with zero-fill severity counts.
3. Extend repository contract and SQL implementation to aggregate counts by UTC date and severity for the requested lookback window.
4. Update focused controller/service/integration tests for boundaries, ordering, zero-fill, and invalid-query responses.
5. Run scoped build and tests from `implementation-cache.json`.

## Notes
- Preserve existing controller -> service -> repository layering and current severity ordering conventions.
