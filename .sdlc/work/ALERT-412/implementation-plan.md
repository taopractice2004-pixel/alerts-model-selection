# Implementation Plan — ALERT-412

Authoritative file inventory, validation commands, and design decisions live in
`implementation-cache.json`. This plan sequences the work; it does not repeat those lists.

## Change Strategy
- Add the new `/api/alerts/trends` read endpoint as a sibling of the existing
  `/api/alerts/summary` endpoint, following the same layering split: repository returns raw
  aggregated counts, service shapes/zero-fills the DTO, controller is a thin pass-through.
- Reuse `AlertSeverityCountsResponse` (Low/Medium/High/Critical) unchanged instead of introducing
  a parallel severity-count shape.
- Reuse the existing DataAnnotations `[Range]` + automatic `[ApiController]` model-validation
  path (already used by `AlertQueryRequest.Page`/`PageSize`) for the `days` query parameter
  instead of hand-rolled validation.

## Sequencing
1. **Constants** — add `AlertConstants.DefaultTrendsDays` (7), `MinTrendsDays` (1),
   `MaxTrendsDays` (90).
2. **Request DTO** — add `AlertService.DTO/Requests/AlertTrendsQueryRequest.cs` with
   `[Range(AlertConstants.MinTrendsDays, AlertConstants.MaxTrendsDays)] int Days =
   AlertConstants.DefaultTrendsDays`.
3. **Response DTOs** — add `AlertTrendBucketResponse` (Date, TotalCount, SeverityCounts) and
   `AlertTrendsResponse` (`IReadOnlyList<AlertTrendBucketResponse> Buckets`) under
   `AlertService.DTO/Responses/`.
4. **Repository contract + implementation** — extend `IAlertRepository` with
   `GetTrendsAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken)` returning raw
   non-zero `(Date, Severity, Count)` rows (EF Core `GroupBy` over `CreatedDate.Date` and
   `Severity`, same aggregation style as `GetSummaryAsync`); implement in `AlertRepository`.
5. **Service layer** — add `IAlertService.GetTrendsAsync(AlertTrendsQueryRequest, CancellationToken)`;
   implement in `AlertManagementService`: compute `today = _timeProvider.GetUtcNow().UtcDateTime.Date`,
   `fromDateUtc = today.AddDays(-(request.Days - 1))`, call the repository, then build `Days`
   buckets oldest-first, zero-filling every day and every `Severity` value not present in the
   repository result.
6. **Controller** — add `[HttpGet("trends")] GetTrends([FromQuery] AlertTrendsQueryRequest
   request, CancellationToken cancellationToken)` returning `Ok(await
   _alertService.GetTrendsAsync(request, cancellationToken))`; invalid `days` short-circuits to
   400 automatically via existing `[ApiController]` model-state behavior (no explicit branching
   needed, matching how `AlertQueryRequest` validation already works for `GetAll`).
7. **Tests** — extend `AlertRepositoryTests` (day/severity grouping, empty range), extend
   `AlertManagementServiceTests` (default days, explicit days, zero-fill, ordering), extend
   `AlertsControllerTests` (200 happy path; 400 for `days=0`, `days=91`, non-numeric `days`).
8. **Validation** — `dotnet build AlertService.sln`, `dotnet test`.

## Validation Strategy
- `dotnet test` is sufficient and narrowest: no schema/migration change, so
  `AlertService.Data.SQL.Tests` + `AlertService.API.Tests` cover repository, service, and
  controller layers without needing a running SQL Server instance (existing tests use EF Core
  InMemory/Sqlite).

## Notes
- Exact files and executable commands are owned by `implementation-cache.json`.
- No new third-party dependency required.
