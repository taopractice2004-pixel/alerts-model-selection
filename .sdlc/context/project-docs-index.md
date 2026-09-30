# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Solution overview, folder structure, dependency flow, API endpoint table, run/build/migration instructions |
| `standards/coding-standards.md` | Engineering standard | PR/review checklist: quality, reliability, security, testing, coverage |
| `standards/backend-dotnet-standards.md` | Engineering standard | .NET naming, layout, language usage, layered architecture guidance |
| `standards/api-rest-standards.md` | Engineering standard | REST/OpenAPI contract, URI/method design, errors, .NET versioning |
| `standards/service-architecture-standards.md` | Engineering standard | Microservice design, .NET API/BFF layering, review checklist |
| `standards/database-standards.md` | Engineering standard | Query/procedure/modeling/naming rules for SQL + EF |
| `standards/frontend-react-standards.md` | Engineering standard | React component/state/styling rules (not currently exercised) |
| `standards/ui-standards.md` | Engineering standard | HTML5/CSS/accessibility rules (not currently exercised) |
| `database/01_CreateDatabase.sql` | Operational script | Creates `AlertServiceDb` |
| `database/02_AlertServiceDb_Migrations.sql` | Operational script | Idempotent migration script generated from EF migrations |
| `.github/prompts/setup-repo-context.prompt.md` | SDLC command doc | Repository cache creation workflow |
| `.github/prompts/refresh-repo-context.prompt.md` | SDLC command doc | Repository cache refresh workflow |
| `.github/prompts/analyze-story.prompt.md` | SDLC command doc | Story cache creation workflow |
| `.github/prompts/implement-story.prompt.md` | SDLC command doc | Implementation-stage workflow |
| `.github/prompts/fix-bugs.prompt.md` | SDLC command doc | Focused standalone or story-related bug-fix workflow |
| `.github/prompts/unit-testing.prompt.md` | SDLC command doc | Focused unit-test creation and validation workflow |
| `.sdlc/README.md` | SDLC framework doc | High-level overview of the compact pipeline structure |

## Missing Or Undiscovered Project Docs

- BRDs or formal business requirement documents: `NOT_AVAILABLE` until discovered
- ADRs / architecture decision records: `NOT_AVAILABLE` until discovered
- CI pipeline documentation: `NOT_AVAILABLE` until discovered
- Deployment runbook: `NOT_AVAILABLE` until discovered
- Checked-in OpenAPI/Swagger spec: `NOT_AVAILABLE` (generated at runtime via Swashbuckle)

## Starter Notes

- Index only. Do not copy document bodies into it.
- `frontend-react-standards.md` and `ui-standards.md` exist but the current solution is a
  backend API with no frontend; apply them only if a UI is added.