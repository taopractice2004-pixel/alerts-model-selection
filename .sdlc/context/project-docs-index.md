# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | AlertService overview, folder structure, API reference, run/test/EF-migration commands, health checks |
| `standards/coding-standards.md` | Engineering standard | Global coding/review checklist |
| `standards/backend-dotnet-standards.md` | Engineering standard | C#/.NET naming, layout, layering, practices |
| `standards/api-rest-standards.md` | Engineering standard | REST contract, URI, HTTP methods, errors, versioning |
| `standards/database-standards.md` | Engineering standard | Schema/query/EF, naming, rollback discipline |
| `standards/service-architecture-standards.md` | Engineering standard | Microservice design + internal .NET layering |
| `standards/frontend-react-standards.md` | Engineering standard | React/frontend rules (no frontend code in repo yet) |
| `standards/ui-standards.md` | Engineering standard | HTML5/CSS/accessibility (no UI markup in repo yet) |
| `database/01_CreateDatabase.sql` | Operational SQL | Creates `AlertServiceDb` |
| `database/02_AlertServiceDb_Migrations.sql` | Operational SQL | Idempotent EF migrations script |
| `.github/skills/*/SKILL.md` | SDLC command doc | Authoritative per-stage procedures |
| `.github/prompts/*.prompt.md` | SDLC command doc | Thin compatibility wrappers delegating to skills |
| `.sdlc/README.md` | SDLC framework doc | High-level overview of the compact pipeline structure |

## Missing Or Undiscovered Project Docs

- BRDs or formal business requirement documents: `NOT_AVAILABLE` until discovered
- ADRs / architecture decision records: `NOT_AVAILABLE` until discovered
- CI pipeline documentation: `NOT_AVAILABLE` until discovered
- Deployment runbook: `NOT_AVAILABLE` until discovered
- Committed OpenAPI/Swagger contract file: `NOT_AVAILABLE` (Swagger generated at runtime)

## Starter Notes

- Index only. Do not copy document bodies into it.