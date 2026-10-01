# Implementation Plan - ALERT-411

## Change Strategy
- Extend the existing create flow rather than introducing a new subsystem: add one repository lookup for recent active duplicates, let the service decide create-versus-suppress, and let the controller choose `200` + header versus `201 Created`.
- Keep duplicate detection narrow and deterministic: same trimmed title ignoring case, same severity, active prior alert only, and created-date within the configurable recent-window boundary.
- Read the suppression window from application configuration using the repository's current ASP.NET Core configuration patterns; avoid hardcoded minute values.

## Validation Strategy
- Run `dotnet test AlertService.API.Tests\AlertService.API.Tests.csproj` and `dotnet test AlertService.Data.SQL.Tests\AlertService.Data.SQL.Tests.csproj` for the touched controller/service/repository slice, then `dotnet build AlertService.sln` to confirm the solution still compiles.

## Notes
- Expect the create service contract to widen enough for the controller to distinguish created versus suppressed outcomes without duplicating business logic.
- Exact files and executable commands are owned by `implementation-cache.json`.
