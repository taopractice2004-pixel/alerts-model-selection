# Implementation Plan - ALERT-412

## Change Strategy
- Add a dedicated query DTO for `days` so the new endpoint inherits the repository's existing
  `[ApiController]` + data-annotation validation behavior, including `ValidationProblemDetails`
  for invalid or non-numeric query values.
- Extend the existing alerts controller/service/repository path with a trends-specific method
  rather than introducing a parallel reporting subsystem.
- Let the repository return grouped counts for the requested UTC date window, then let the
  service apply the default day count, generate missing day buckets, zero-fill missing
  severities, and preserve the established severity order from the summary endpoint.
- Add focused response DTOs for the day buckets and overall trends payload, reusing the existing
  severity-count shape when practical to keep the API contract consistent.

## Validation Strategy
- Run `dotnet test AlertService.API.Tests\AlertService.API.Tests.csproj` for controller and
  service coverage of response shaping, defaulting, and validation-adjacent behavior.
- Run `dotnet test AlertService.Data.SQL.Tests\AlertService.Data.SQL.Tests.csproj` for
  repository aggregation and date-window coverage.
- Run `dotnet build AlertService.sln` to confirm the solution still compiles after the contract
  extension.

## Notes
- Assume "last N days" includes the current UTC day plus the previous `N - 1` UTC calendar days.
- If unit/controller tests do not reliably prove the non-numeric query binding path, add one
  focused `WebApplicationFactory` API test during implementation and update the cache then.
- Exact file inventory and executable commands are owned by `implementation-cache.json`.
