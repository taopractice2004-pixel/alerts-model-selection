# Implementation Plan — ALERT-412

## Approach
Add a read-only trends slice that mirrors the existing summary-endpoint pattern
(Controller → Service → `IAlertRepository` → EF Core), reusing `AlertSeverityCountsResponse`
for per-bucket severity shape/ordering.

1. **DTOs** (`AlertService.DTO`): add `AlertTrendsQueryRequest` (`Days` int, `[Range(1,90)]`,
   default 7) and response types `AlertTrendsResponse` (wraps ordered bucket list) +
   `AlertTrendBucketResponse` (`DateOnly Date`, `int TotalCount`, `AlertSeverityCountsResponse
   SeverityCounts`).
2. **Repository** (`IAlertRepository` / `AlertRepository`): add a method returning raw per-day,
   per-severity counts for `[startUtc, endUtc)`, grouping by `CreatedDate.Date` and `Severity`
   in a single query (no `SELECT *`, set-based per `database-standards.md`).
3. **Service** (`AlertManagementService`): compute the `N`-day UTC window using the injected
   `TimeProvider` (matching the `CreateAsync` convention — not `DateTime.UtcNow` directly),
   fetch raw counts, then zero-fill every day/severity combination missing from the raw result
   so every bucket is present oldest-first.
4. **Controller** (`AlertsController`): add `[HttpGet("trends")] GetTrends([FromQuery]
   AlertTrendsQueryRequest request, ...)`; rely on `[ApiController]` automatic model-state
   validation for the `400 ValidationProblemDetails` response (same convention as the existing
   `GetAll` action) — no manual validation branch needed.
5. **Constants**: add `AlertConstants` entries for default/min/max trend days instead of magic
   numbers.

## Validation
- `dotnet build AlertService.sln`
- `dotnet test AlertService.sln --no-build`

See `implementation-cache.json` for the authoritative file list, constraints, and unresolved
questions.
