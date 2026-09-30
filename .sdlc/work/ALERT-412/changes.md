# Changes — ALERT-412

## Stage Summary
- Timestamp: 2026-09-30T00:00:00Z
- Command: /implement-story
- Files changed:
  - AlertService.Common/Constants/AlertConstants.cs
  - AlertService.Data/Interfaces/IAlertRepository.cs
  - AlertService.Data.SQL/Repositories/AlertRepository.cs
  - AlertService.DTO/Requests/AlertTrendsQueryRequest.cs (new)
  - AlertService.DTO/Responses/AlertTrendsResponse.cs (new)
  - AlertService.DTO/Responses/AlertTrendBucketResponse.cs (new)
  - AlertService.API/Services/IAlertService.cs
  - AlertService.API/Services/AlertManagementService.cs
  - AlertService.API/Controllers/AlertsController.cs
  - AlertService.API.Tests/Services/AlertManagementServiceTests.cs
  - AlertService.API.Tests/Controllers/AlertsControllerTests.cs
  - AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs
- Description: Added `GET /api/alerts/trends?days=N` (default 7, min 1, max 90). New
  `AlertTrendsQueryRequest` DTO uses `[Range(AlertConstants.TrendsMinDays,
  AlertConstants.TrendsMaxDays)]` on `Days`, so out-of-range/non-numeric input yields the
  standard `[ApiController]` auto `400 ValidationProblemDetails` (no manual validation branch).
  `IAlertRepository.GetTrendCountsAsync` runs a single set-based `GroupBy(Date, Severity)`
  query over `[startInclusiveUtc, endExclusiveUtc)`. `AlertManagementService.GetTrendsAsync`
  computes the UTC day window from the injected `TimeProvider` (matches the `CreateAsync`
  convention), then builds one `AlertTrendBucketResponse` per day oldest-first, zero-filling
  missing days/severities and reusing `AlertSeverityCountsResponse` for per-bucket severity
  ordering (Low/Medium/High/Critical). `AlertsController.GetTrends` delegates to the service and
  returns `200 OK` with `AlertTrendsResponse`.

## Unresolved-Question Resolutions
- None — cache had no unresolved questions for this story.

## Validation Results
- `dotnet build AlertService.sln` — succeeded (all 8 projects).
- `dotnet test AlertService.sln --no-build` — 93/93 passed.

## Coverage Results
- NOT_CONFIGURED (no coverage tooling in repo; behavior coverage added via new unit tests:
  default days=7 window computation, explicit days within range with zero-fill for missing
  days/severities, bucket ordering oldest-first, severity ordering per bucket, repository
  grouping restricted to the given UTC range (start inclusive/end exclusive), controller
  200 mapping, and `AlertTrendsQueryRequest` boundary/out-of-range validation).

## Reproduction Results
- NOT_RUN (no reproduction commands defined for this story).

## Standards Applied
- coding-standards.md, backend-dotnet-standards.md, api-rest-standards.md,
  service-architecture-standards.md, database-standards.md.

## Deferred Items
- None.
