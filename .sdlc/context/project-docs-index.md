# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Primary overview of architecture, endpoints, prerequisites, local run steps, health checks, and EF migration commands |
| `database\01_CreateDatabase.sql` | Database bootstrap script | Creates `AlertServiceDb` before EF migrations or the idempotent SQL script are applied |
| `database\02_AlertServiceDb_Migrations.sql` | Database deployment script | Generated idempotent SQL for the current EF Core migration set |
| `standards\coding-standards.md` | Engineering standard | Global coding, review, reliability, and testing expectations |
| `standards\backend-dotnet-standards.md` | Engineering standard | C#/.NET naming, layout, language, and layering guidance |
| `standards\api-rest-standards.md` | Engineering standard | REST resource, contract, error, and versioning guidance |
| `standards\database-standards.md` | Engineering standard | Database review, modeling, query, and naming guidance |
| `standards\service-architecture-standards.md` | Engineering standard | Service layering, autonomy, operational, and API/BFF review guidance |
| `standards\frontend-react-standards.md` | Engineering standard | Reusable frontend guidance available for future React/JS work |
| `standards\ui-standards.md` | Engineering standard | HTML/CSS/accessibility guidance available for future markup/styling work |
| `.sdlc\README.md` | SDLC framework doc | Overview of the packaged manual SDLC workflow included in this repository |

## Missing Or Undiscovered Project Docs

- Architecture decision records: `NOT_AVAILABLE`
- CI pipeline documentation: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Standalone OpenAPI contract file: `NOT_AVAILABLE`