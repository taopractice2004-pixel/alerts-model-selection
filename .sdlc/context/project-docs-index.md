# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Solution overview, folder structure, dependency flow, API/health-check reference, local run + EF migration commands |
| `standards/coding-standards.md` | Engineering standard | General PR/code-review checklist (naming, quality, reliability/security, testing, PR/reviewer expectations) |
| `standards/backend-dotnet-standards.md` | Engineering standard | .NET/C# naming, file layout, language usage, layering guidance |
| `standards/api-rest-standards.md` | Engineering standard | REST contract-first, resource/URI design, HTTP methods, versioning, backward compatibility |
| `standards/service-architecture-standards.md` | Engineering standard | Microservice design principles, internal layering, API/BFF review checklist |
| `standards/database-standards.md` | Engineering standard | DB review process, query/procedure guidance, modeling and naming conventions |
| `standards/frontend-react-standards.md` | Engineering standard | React component/state/styling conventions (not currently applicable — no frontend project in this repo) |
| `standards/ui-standards.md` | Engineering standard | HTML5/accessibility/CSS conventions (not currently applicable — no UI project in this repo) |
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

## Starter Notes

- `/setup-repo-context` should replace the placeholder purpose values with repository-specific
  metadata.
- Keep this file as an index only. Do not copy document bodies into it.