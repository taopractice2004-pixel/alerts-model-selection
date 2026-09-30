# Project Documentation Index

Key documentation and reference files found in the repository:

- `README.md` — repository overview and quickstart (root)
- `AlertService.API/Program.cs` and `launchSettings.json` — runtime entry and local launch
- `standards/*` — team standards for API, backend .NET, coding, database, frontend, UI, architecture
- `database/01_CreateDatabase.sql` and `02_AlertServiceDb_Migrations.sql` — DB setup and migration scripts
- `AlertService.API.http` — sample API requests (HTTP collection)
- Tests: `AlertService.API.Tests/`, `AlertService.Data.SQL.Tests/` — unit/integration tests

If additional docs (design decisions, ADRs, or ops runbooks) are added, include them under a documented `docs/` or update this index.
# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product / setup doc | `TO_BE_DISCOVERED` |
| `docs/<file>` | Architecture / product / runbook doc | `TO_BE_DISCOVERED` |
| `standards/<file>.md` | Engineering standard | `TO_BE_DISCOVERED` |
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