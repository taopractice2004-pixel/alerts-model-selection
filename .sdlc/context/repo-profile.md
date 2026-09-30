# Repository Profile

**Name:** .net-demo-with-vscode (AlertService)

**Primary Language/Platform:** C# / .NET (SDK-style projects)

**Top-level Build/Test Commands:**
- `dotnet build` (solution: AlertService.sln)
- `dotnet test` (test projects under AlertService.API.Tests and AlertService.Data.SQL.Tests)

**Solution / Projects:**
- `AlertService.sln` (root solution)
- `AlertService.API` (ASP.NET Core Web API)
- `AlertService.Common` (shared constants, enums)
- `AlertService.Data.SQL` (EF Core DbContext, repositories, migrations)
- `AlertService.DTO` (requests/responses)
- `AlertService.Models` (domain models)
- Test projects: `AlertService.API.Tests`, `AlertService.Data.SQL.Tests`

**CI/CD / Operational Notes:**
- No CI config discovered in repo root (TO_BE_DISCOVERED) — look for `.github/workflows/` if present later.

**Standards Present:** `standards/` (API, backend .NET, coding, database, frontend, UI, architecture)

**Repository Mode:** MODE_C_EXISTING_PROJECT

**Notes / Unknowns:**
- Secret management, deployment pipeline, and environment-specific configuration locations: TO_BE_DISCOVERED
# Repository Profile

- Repository mode: `TO_BE_DISCOVERED`
- Product: `TO_BE_DISCOVERED`
- Primary stack: `TO_BE_DISCOVERED`
- Repository shape: `TO_BE_DISCOVERED`
- Shared build/runtime settings: `TO_BE_DISCOVERED`

## Architecture

- Layering: `TO_BE_DISCOVERED`
- Entry points / composition roots: `TO_BE_DISCOVERED`
- User-facing or external interfaces: `TO_BE_DISCOVERED`
- Business logic locations: `TO_BE_DISCOVERED`
- Data / integration boundaries: `TO_BE_DISCOVERED`
- Runtime / infrastructure notes: `TO_BE_DISCOVERED`

## Build And Run

- Install / restore dependencies: `TO_BE_DISCOVERED`
- Build / compile command: `TO_BE_DISCOVERED`
- Test command: `TO_BE_DISCOVERED`
- Lint / format command: `TO_BE_DISCOVERED`
- Run main app locally: `TO_BE_DISCOVERED`

## Data And Operations

- Primary data store: `TO_BE_DISCOVERED`
- Environment/config notes: `TO_BE_DISCOVERED`
- Operational assets: `TO_BE_DISCOVERED`

## Testing

- Main test layers: `TO_BE_DISCOVERED`
- Test frameworks: `TO_BE_DISCOVERED`

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE` until discovered
- Deployment/runbook documentation: `NOT_AVAILABLE` until discovered
- Separate API contract artifacts: `NOT_AVAILABLE` until discovered

## Starter Notes

- This is a compact starter file for the current pipeline.
- `/setup-repo-context` should overwrite it with repository-specific facts.
- `/implement-story` should use this file only when the story cache does not already contain
  the needed fact.