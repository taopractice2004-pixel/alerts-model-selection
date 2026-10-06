# ALERT-412: Add alert trends endpoint

## PR Description
Add a trends reporting endpoint that returns daily UTC alert-creation counts for the last N days, including total and per-severity buckets in the existing Low, Medium, High, Critical order. The implementation keeps the existing controller -> service -> repository layering and uses the API's standard query validation behavior for invalid `days` input.

## Story / Requirement Summary
- AC1: Return one UTC day bucket per requested day, oldest first, with total and per-severity counts.
- AC2: Zero-fill missing days and missing severities instead of omitting them.
- AC3: Default `days` to 7 and only allow values from 1 through 90.
- AC4: Return `400 ValidationProblemDetails` for out-of-range and non-numeric `days` values.
- AC5: Keep per-severity counts ordered as Low, Medium, High, Critical.

## Implementation Summary
- Added `GET /api/alerts/trends` in the alerts controller and extended the alert service contract with a trends query method.
- Implemented service-side UTC window calculation, chronological bucket shaping, and zero-fill behavior for missing days and severities.
- Added repository-side daily aggregation by UTC date with total and per-severity counts, plus dedicated request/response DTOs and shared trend-day constants.

## Changed-Files Summary
Diff source: `git status --porcelain` scoped to ALERT-412 files.

- Production: `AlertService.Common/Constants/AlertConstants.cs`, `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/IAlertService.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data/Interfaces/IAlertRepository.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.DTO/Requests/AlertTrendQueryRequest.cs`, `AlertService.DTO/Responses/AlertTrendBucketResponse.cs`
- Tests: `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.API.Tests/AlertTrendsEndpointTests.cs`, `AlertService.API.Tests/Services/AlertManagementServiceTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`
- SDLC artifacts: `.sdlc/work/ALERT-412/work.json`, `.sdlc/work/ALERT-412/plan.md`, `.sdlc/work/ALERT-412/log.md`, `.sdlc/work/ALERT-412/pr.md`

## Acceptance Criteria Traceability
- AC1:
  - Implemented in `AlertService.API/Controllers/AlertsController.cs`, `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data/Interfaces/IAlertRepository.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.DTO/Responses/AlertTrendBucketResponse.cs`
  - Proven by `GetTrends_ReturnsOkWithTypedBuckets`, `GetTrendsAsync_UsesDefaultSevenDayWindow_WhenDaysNotSpecified`, `GetDailyTrendsAsync_GroupsByUtcDay_ReturnsSeverityBreakdown_AndUsesExclusiveEndBoundary`
- AC2:
  - Implemented in `AlertService.API/Services/AlertManagementService.cs`, `AlertService.DTO/Responses/AlertTrendBucketResponse.cs`
  - Proven by `GetTrendsAsync_UsesDefaultSevenDayWindow_WhenDaysNotSpecified`, `GetTrendsAsync_ZeroFillsMissingDays_AndMapsSeverityCountsInOrder`
- AC3:
  - Implemented in `AlertService.Common/Constants/AlertConstants.cs`, `AlertService.DTO/Requests/AlertTrendQueryRequest.cs`, `AlertService.API/Services/AlertManagementService.cs`
  - Proven by `AlertTrendQueryRequest_UsesSevenDayDefault`, `AlertTrendQueryRequest_WithOutOfRangeDays_FailsValidation`, `GetTrendsAsync_UsesDefaultSevenDayWindow_WhenDaysNotSpecified`
- AC4:
  - Implemented in `AlertService.DTO/Requests/AlertTrendQueryRequest.cs`, `AlertService.API/Controllers/AlertsController.cs`
  - Proven by `AlertTrendQueryRequest_WithOutOfRangeDays_FailsValidation`, `GetTrends_WithInvalidDays_ReturnsValidationProblem`
- AC5:
  - Implemented in `AlertService.API/Services/AlertManagementService.cs`, `AlertService.Data.SQL/Repositories/AlertRepository.cs`, `AlertService.DTO/Responses/AlertTrendBucketResponse.cs`
  - Proven by `GetTrends_ReturnsOkWithTypedBuckets`, `GetTrendsAsync_ZeroFillsMissingDays_AndMapsSeverityCountsInOrder`, `GetDailyTrendsAsync_GroupsByUtcDay_ReturnsSeverityBreakdown_AndUsesExclusiveEndBoundary`

## Test / Build Results Already Recorded
- Build: `dotnet build AlertService.API/AlertService.API.csproj` - PASSED during `/implement-story`
- API/controller/service tests: `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter "FullyQualifiedName~AlertsControllerTests|FullyQualifiedName~AlertTrendsEndpointTests|FullyQualifiedName~AlertManagementServiceTests"` - PASSED (65/65)
- Repository tests: `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"` - PASSED (34/34)
- Coverage: `AlertService.API` scoped XPlat coverage collected (line-rate 87.24%, branch-rate 80.76%); `AlertService.Data.SQL.Tests` coverage is NOT_CONFIGURED because the project does not reference a supported XPlat code coverage collector

## Configuration / Database / Migration Impacts
None.

## Known Risks / Limitations
None.