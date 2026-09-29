# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Solution overview, API surface, local run/test flow, health checks, migrations, and design notes |
| `standards/coding-standards.md` | Engineering standard | Cross-cutting coding, review, testing, and PR expectations |
| `standards/backend-dotnet-standards.md` | Engineering standard | C#/.NET naming, layout, language, and layering rules |
| `standards/api-rest-standards.md` | Engineering standard | REST contract, URI, versioning, and error-response conventions |
| `standards/service-architecture-standards.md` | Engineering standard | Microservice and internal service-layer boundary guidance |
| `standards/database-standards.md` | Engineering standard | Schema, query, naming, rollback, and performance review rules |
| `standards/frontend-react-standards.md` | Engineering standard | React component, state, styling, and review rules for any future frontend work |
| `standards/ui-standards.md` | Engineering standard | HTML/CSS semantics and accessibility rules for any future UI work |
| `.github/prompts/setup-repo-context.prompt.md` | SDLC command doc | Repository cache creation workflow |
| `.github/prompts/refresh-repo-context.prompt.md` | SDLC command doc | Repository cache refresh workflow |
| `.github/prompts/analyze-story.prompt.md` | SDLC command doc | Story cache creation workflow |
| `.github/prompts/implement-story.prompt.md` | SDLC command doc | Implementation-stage workflow |
| `.github/prompts/fix-bugs.prompt.md` | SDLC command doc | Focused bug-fix workflow |
| `.github/prompts/create-testcases.prompt.md` | SDLC command doc | Focused unit-test creation workflow |
| `.github/copilot-instructions.md` | Repo automation rules | Global workflow, traceability, and stage-behavior rules for all agents |
| `.sdlc/README.md` | SDLC framework doc | Overview of cached artifacts, stage sequence, and work-folder contents |

## Missing Or Undiscovered Project Docs

- BRDs or formal business requirement documents: `NOT_AVAILABLE`
- ADRs / architecture decision records: `NOT_AVAILABLE`
- CI pipeline documentation: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- OpenAPI source file checked into the repo: `NOT_AVAILABLE`

## Starter Notes

- Keep this file as an index only; use the standards summary for rule selection.
- No separate `docs/` tree was discovered at repository root during this setup pass.