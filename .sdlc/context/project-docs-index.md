# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Architecture, API routes, health checks, prerequisites, local run, tests, and EF commands |
| `standards/*.md` | Engineering standards | Repository coding, backend/API/service/database, and conditional frontend/UI guidance; summarized in `standards-summary.md` |
| `database/01_CreateDatabase.sql` | Database script | Creates the AlertService database |
| `database/02_AlertServiceDb_Migrations.sql` | Database script | Idempotent migration script for database deployment |
| `Directory.Build.props` | Build configuration | Shared target framework and compiler settings |
| `dotnet-tools.json` | Tool manifest | Pins `dotnet-ef` 8.0.31 |
| `.github/prompts/*.prompt.md` | SDLC command docs | Repository setup, refresh, story analysis, implementation, bug fixing, and unit testing workflows |
| `.sdlc/framework/*.md` | SDLC framework docs | Workflow, stage rules, context reuse, and pipeline guidance |
| `.sdlc/README.md` | SDLC framework doc | Compact pipeline overview |

## Missing Or Undiscovered Project Docs

- BRDs or formal business requirement documents: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI pipeline documentation: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Committed OpenAPI contract: `NOT_AVAILABLE` (Swagger is generated at runtime in Development)