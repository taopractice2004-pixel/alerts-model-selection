# Implementation Plan — ALERT-412

## Change Strategy
- Add a new trends contract in controller + service for `GET /api/alerts/trends` with query
  parameter `days` constrained to `1..90` and default `7`.
- Extend service and repository contracts with a focused daily-aggregation read path that:
  - groups alerts by UTC creation date for the last N days,
  - returns oldest-first day buckets,
  - includes zero-filled buckets and zero-filled severities,
  - emits severity counts in existing summary order (`Low`, `Medium`, `High`, `Critical`).
- Introduce minimal DTOs for trends request/response shapes aligned with existing API response
  patterns.

## Validation Strategy
- Run focused controller/service/repository unit tests for:
  - default and bounded `days` behavior,
  - severity/total counts and ordering,
  - zero-filled day/severity buckets,
  - oldest-first bucket ordering.
- Run solution build and focused tests for touched layers.

## Notes
- Exact file and command lists are authoritative in `implementation-cache.json`.
