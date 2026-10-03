# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, keep tests focused and deterministic | None stated until checked | Global / highest |
| `<backend standard file if present>` | Backend / service code | Touching backend logic, APIs, jobs, middleware, or server-side tests | Preserve layering, naming/layout conventions, and operational safety practices | Validate against target repository versions and frameworks | High when present |
| `<api standard file if present>` | API / external contract design | Changing routes, request/response contracts, events, or externally consumed interfaces | Keep contracts consistent and version changes deliberate | Validate protocol/versioning guidance against target repository | High when present |
| `<service architecture standard file if present>` | Internal architecture | Changing boundaries, orchestration, async flows, or integrations | Keep clear boundaries between delivery, business, and persistence/integration layers | Validate architecture-specific guidance in context | High when present |
| `<database standard file if present>` | Database and persistence | Changing schema, queries, migrations, repositories, or ORM behavior | Avoid risky query patterns, review indexing/performance, preserve naming and rollback discipline | Validate against target repository database technology | High when present |
| `<frontend standard file if present>` | Frontend / client app | Touching UI components, state, routing, or client-side data flows | Keep components focused, separate concerns, preserve accessibility and maintainability | Validate against target repository UI stack | Conditional |
| `<ui standard file if present>` | Markup / styling / accessibility | Touching markup, styling, layout, or user-facing presentation | Prefer semantic structure, accessible content, and maintainable styling | Validate against target repository rendering technology | Conditional |

## Selection Rules

- Always apply the repository's general coding standards when present.
- Apply backend, API, service, database, frontend, or UI standards only when the story touches those areas and those standards exist in the target repository.

## Standards Loading Model

Standards now use a three-tier split to minimize repeated context:

- `standards/*.md` — full source of truth. Read a full file only when an exact rule or missing
  detail is required.
- `.github/instructions/standards/*.instructions.md` — compact, AI-enforceable rules that
  Copilot auto-applies via `applyTo` globs for matching files. These never duplicate the full
  document.
- `.sdlc/context/standards-index.json` — routing index mapping each standard `id` to its source
  file, compact instruction file, and applicable file types.

Stages record the relevant standard ids in `implementation-cache.json` under
`selected_standards` (for example `["coding", "backend-dotnet", "api-rest"]`). Downstream
stages rely on the cache plus the auto-applied compact instruction files and do not reread the
entire `standards/` folder.

## Starter Notes

- `/setup-repo-context` should confirm this summary and `standards-index.json` against the
  bundled `standards/` folder and the target repository.
- `/analyze-story` should select only the relevant standard ids (from `standards-index.json`)
  into `implementation-cache.json`.
- `/implement-story`, `/fix-bugs`, and `/unit-testing` should rely on the selected ids and the
  auto-applied compact instruction files, reading a full `standards/*.md` file only for an exact
  rule or missing detail.