# Changes - ALERT-412

- Added `GET /api/alerts/trends` with a validated `days` query parameter (default 7, range 1-90).
- Added UTC half-open date-range aggregation by severity through the controller, service, repository abstraction, and EF/SQL repository.
- Added oldest-first response materialization with zero-count dates and Low/Medium/High/Critical fields.
- Added focused controller, service, and repository tests for validation, UTC boundaries, ordering, zero buckets, severity mapping, and aggregation.

Validation:
- `dotnet build AlertService.sln` passed.
- `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter FullyQualifiedName~AlertsControllerTests` passed (28 tests).
- `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj --filter FullyQualifiedName~AlertManagementServiceTests` passed (22 tests).
- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter FullyQualifiedName~AlertRepositoryTests` passed (30 tests).