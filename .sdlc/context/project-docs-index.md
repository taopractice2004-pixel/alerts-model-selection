# Project Docs Index

| Path | Type | Purpose |
|---|---|---|
| `README.md` | Product/setup and architecture overview | Defines service purpose, endpoint contract, health probes, local run flow, and migration commands |
| `Directory.Build.props` | Build/runtime config | Shared framework and compiler behavior (`net8.0`, nullable, implicit usings) |
| `dotnet-tools.json` | Tooling manifest | Local CLI tooling (`dotnet-ef`) for migration lifecycle |
| `database/01_CreateDatabase.sql` | Database bootstrap script | Creates database for manual DB provisioning flow |
| `database/02_AlertServiceDb_Migrations.sql` | Database migration artifact | Idempotent SQL migration output for DBA/CI usage |
| `standards/*.md` | Engineering standards (full source) | Canonical coding, API, backend, DB, architecture, frontend, and UI standards |
| `.github/instructions/standards/*.instructions.md` | Compact standards rules | Auto-applied, AI-enforceable summary rules for touched file types |
| `.github/skills/*/SKILL.md` | SDLC stage procedure | Authoritative manual procedures for setup/refresh/analyze/implement/fix/test stages |
| `.github/prompts/*.prompt.md` | Prompt compatibility wrappers | Thin wrappers that delegate stage execution to matching skills |
| `.sdlc/framework/workflow.md` | SDLC lifecycle map | Command order and stage integration model |
| `.sdlc/framework/stage-rules.md` | Stage output contract | Mandatory artifacts and stage-specific write/update rules |
| `.sdlc/framework/context-rules.md` | Context loading contract | Read order, staleness behavior, and exclusions usage |

## Missing Or Undiscovered Project Docs

- BRD or formal product requirement documents: `NOT_AVAILABLE`
- ADR catalog: `NOT_AVAILABLE`
- CI/CD pipeline runbook: `NOT_AVAILABLE`
- Production deployment playbook: `NOT_AVAILABLE`