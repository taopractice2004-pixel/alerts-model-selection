# Changes - ALERT-412

## Implementation
- Added `GET /api/alerts/trends` to the alerts controller with `[ApiController]` query validation for `days` defaulting to `7` and constrained to `1..90`.
- Added trend DTO contracts for the request and bucketed response shape, reusing `AlertSeverityCountsResponse` for ordered Low/Medium/High/Critical counts.
- Extended `IAlertService` and `IAlertRepository` with daily trend retrieval and implemented UTC last-N-day bucketing with zero-filled calendar days in the SQL repository.
- Added focused controller, service, and repository tests covering request validation, controller wiring, zero-filled severity counts, oldest-first ordering, and UTC inclusive day windows.

## Validation
- Attempted: `dotnet test AlertService.API.Tests/AlertService.API.Tests.csproj`.
- Blocked by environment: .NET 8 runtime is not installed on this machine, so `testhost.exe` could not start even though the test project compiled successfully.
- Passed: `dotnet build AlertService.sln`.