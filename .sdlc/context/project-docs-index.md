# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Repository overview, architecture, API surface, local setup, health checks, and EF migration commands |
| `database/01_CreateDatabase.sql` | Database setup script | Creates the SQL Server database before applying migrations |
| `database/02_AlertServiceDb_Migrations.sql` | Database deployment script | Idempotent EF-generated schema script for DB deployment workflows |
| `standards/coding-standards.md` | Engineering standard | Cross-cutting code quality, review, security, and testing expectations |
| `standards/backend-dotnet-standards.md` | Engineering standard | C# naming, file layout, language usage, and backend layering guidance |
| `standards/api-rest-standards.md` | Engineering standard | REST contract, URI, response/error, and versioning rules |
| `standards/service-architecture-standards.md` | Engineering standard | Service decomposition, layering, DI, async, logging, and coverage checks |
| `standards/database-standards.md` | Engineering standard | Database review, query, naming, and performance expectations |
| `standards/frontend-react-standards.md` | Engineering standard | Frontend React conventions for component, state, and styling work |
| `standards/ui-standards.md` | Engineering standard | HTML/CSS semantics, accessibility, and styling guidance |
| `.github/copilot-instructions.md` | SDLC framework doc | Global repository rules for the Copilot-based workflow |
| `.github/prompts/setup-repo-context.prompt.md` | SDLC command doc | Repository cache creation workflow |
| `.github/prompts/refresh-repo-context.prompt.md` | SDLC command doc | Repository cache refresh workflow |
| `.github/prompts/analyze-story.prompt.md` | SDLC command doc | Story cache creation workflow |
| `.github/prompts/implement-story.prompt.md` | SDLC command doc | Implementation workflow |
| `.github/prompts/fix-bugs.prompt.md` | SDLC command doc | Focused bug-fix workflow |
| `.github/prompts/unit-testing.prompt.md` | SDLC command doc | Focused unit-test creation workflow |
| `.sdlc/README.md` | SDLC framework doc | Overview of the compact cache-first pipeline structure |

## Missing Or Undiscovered Project Docs

- BRDs or formal business requirement documents: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI pipeline documentation: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Separate checked-in OpenAPI contract file: `NOT_AVAILABLE`
