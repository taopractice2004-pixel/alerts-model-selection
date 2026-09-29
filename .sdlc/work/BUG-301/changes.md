# BUG-301 Changes

- Replaced direct severity ordering in AlertRepository with an explicit business-rank expression so SQL-backed queries sort Critical > High > Medium > Low instead of relying on lexical string ordering.
- Added SQLite-backed repository regression coverage for severity sorting so the test exercises relational translation rather than EF InMemory-only behavior.
- Added the EF Core SQLite provider to the repository test project to support the relational regression test.

## Validation

- `dotnet test AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj --filter "FullyQualifiedName~GetAllAsync_WithSortBySeverity"` : build succeeded, test execution blocked because the machine is missing the .NET 8 shared runtime required by `testhost.exe`.
- `dotnet build AlertService.sln` : succeeded.
- Editor diagnostics for touched files: no errors.
