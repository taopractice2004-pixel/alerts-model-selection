# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | Folder structure, dependency flow, API/health endpoint reference, local run + EF migration commands, design notes |
| `standards/*.md` | Engineering standard | Full-source coding/backend/API/database/service-architecture/frontend/UI standards (see `standards-index.json`) |
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

- Keep this file as an index only. Do not copy document bodies into it.
- `/refresh-repo-context` should update this index when docs are added, removed, or renamed.