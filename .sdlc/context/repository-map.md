# Repository Map

Top-level folders and short descriptions:

- `AlertService.API/`: ASP.NET Core Web API project; controllers, middleware, services, mappings.
- `AlertService.Common/`: Shared constants and enums.
- `AlertService.Data.SQL/`: EF Core `AlertDbContext`, migrations, repositories.
- `AlertService.DTO/`: DTO request/response types.
- `AlertService.Models/`: Domain model classes (e.g., `Alert`).
- `AlertService.API.Tests/`: Unit and integration tests for the API.
- `AlertService.Data.SQL.Tests/`: Tests for data-layer logic.
- `database/`: SQL scripts and migration helper scripts.
- `standards/`: Team standards and guidelines (coding, API, DB, UI, architecture).
- `.sdlc/` (created by this tool): repository cache and context files.
# Repository Map

## Repository Layout

| Path | Role | Notes |
|---|---|---|
| `<top-level app, package, service, library, or feature path>` | `<role>` | `<why it matters for story scoping>` |

## Runtime Control Points

- Main startup/composition points: `TO_BE_DISCOVERED`
- Main UI/API/CLI/background entry surfaces: `TO_BE_DISCOVERED`
- Main business orchestration points: `TO_BE_DISCOVERED`
- Main persistence or integration boundaries: `TO_BE_DISCOVERED`

## Notable Dependency Flow

- `TO_BE_DISCOVERED`

## Starter Notes

- Record only the important solution areas and control points.
- Keep this compact. It is meant to help `/analyze-story` narrow the impacted slice.
- Do not turn this into a full directory listing.