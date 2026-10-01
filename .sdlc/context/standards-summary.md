# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, handle exceptions deliberately, keep tests focused and deterministic | None stated | Global / highest |
| `backend-dotnet-standards.md` | Backend / .NET code | Touching `.cs`/`.csproj` backend logic, APIs, jobs, or server-side tests | PascalCase types/members, camelCase locals, `I`-prefixed interfaces, one class per file, predictable member ordering | Targets current repo's .NET 8 projects | High when present |
| `api-rest-standards.md` | API / external contract design | Changing controllers, routes, request/response contracts, or OpenAPI/Swagger artifacts | Keep REST contracts consistent, deliberate versioning, controllers stay thin (HTTP concerns only) | Validate against ASP.NET Core 8 conventions already in use | High when present |
| `service-architecture-standards.md` | Internal architecture | Changing `*Service.cs`, `*Client.cs`, `Program.cs`, `Startup.cs`, boundaries, or integrations | Keep clear boundaries between controllers, services, and repositories; service layer depends only on abstractions | Validate against existing layering (API → Service → `IAlertRepository`) | High when present |
| `database-standards.md` | Database and persistence | Changing `*.sql`, `Migrations/`, `*DbContext.cs`, or `*Repository.cs` | Avoid risky query patterns, review indexing/performance, preserve migration and rollback discipline | Validate against EF Core 8 / SQL Server usage already in repo | High when present |
| `frontend-react-standards.md` | Frontend / client app | Touching `.tsx`/`.jsx`/`.ts`/`.js` UI components, state, or client-side data flows | Keep components focused, separate concerns, preserve accessibility and maintainability | Not currently applicable — repository has no frontend project | Conditional (currently unused) |
| `ui-standards.md` | Markup / styling / accessibility | Touching `.html`/`.css`/`.scss` | Prefer semantic structure, accessible content, maintainable styling | Not currently applicable — repository has no markup/styling assets | Conditional (currently unused) |

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

- `/refresh-repo-context` should re-confirm this summary and `standards-index.json` against the
  `standards/` folder when standards are added, removed, or changed.
- `/analyze-story` should select only the relevant standard ids (from `standards-index.json`)
  into `implementation-cache.json`.
- `/implement-story`, `/fix-bugs`, and `/unit-testing` should rely on the selected ids and the
  auto-applied compact instruction files, reading a full `standards/*.md` file only for an exact
  rule or missing detail.