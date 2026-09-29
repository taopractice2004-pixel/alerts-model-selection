# ALERT-201 Changes

## Implemented
- Added optional `CreatedFrom` and `CreatedTo` query properties to `AlertQueryRequest`.
- Added request-level validation so `CreatedFrom > CreatedTo` returns a validation error through the existing API pipeline.
- Threaded the new date-range values through `AlertManagementService` and `IAlertRepository`.
- Applied inclusive `CreatedDate >= createdFrom` and `CreatedDate <= createdTo` filtering in `AlertRepository` before count, sort, and paging.
- Added focused tests for invalid range validation, service pass-through, and inclusive repository lower-bound, upper-bound, and combined-range filtering.
- Expanded repository test coverage to include a bounded range that matches no alerts and a range combined with existing `isActive`, `severity`, and `search` filters.

## Validation
- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~AlertRepositoryTests"` compiled the repository test project successfully but could not execute because the machine is missing the .NET 8 shared runtime required by `testhost.exe`.
- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --collect:"XPlat Code Coverage"` compiled successfully but coverage collection could not run for the same missing .NET 8 shared runtime reason.