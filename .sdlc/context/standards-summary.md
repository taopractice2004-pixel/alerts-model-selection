# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns; no hardcoded values; validate nulls/bounds/failure paths; deliberate exception handling; dispose resources; no secrets in logs; AAA tests; keep >=90% coverage on changed code when the rule applies | None stated | Global / highest |
| `backend-dotnet-standards.md` | Backend / .NET code | Touching C# services, controllers, middleware, DI, or server-side logic | PascalCase types/members, camelCase locals, `I`-prefixed interfaces; braces on new lines; C# aliases; `throw;` not `throw ex;`; short single-purpose methods; return empty collections not null; prefer `Any()`; layered separation, keep DB out of controllers | Target is .NET 8 / C# | High |
| `api-rest-standards.md` | API / external contract design | Changing routes, request/response contracts, status codes, or versioning | Resources as nouns, `/domain/v1/resources`, HTTP methods express action; JSON default; return arrays (incl. empty) for search; RFC7807 error payloads; URL path versioning; version DTOs + controllers | .NET URL path versioning | High |
| `service-architecture-standards.md` | Internal architecture | Changing boundaries, orchestration, async flows, integrations | Keep REST-facing classes separate from service logic; route persistence through service layer; async without `.Wait()`/`.Result`; shared `HttpClient`; correct DI; reflect contract changes in Swagger/OpenAPI | Validate in context | High |
| `database-standards.md` | Database and persistence | Changing schema, EF queries, migrations, repositories | No `SELECT *`; set-based logic; tight transactions; scrutinize EF/LINQ like SQL; default 3NF; naming `IX_/PK_/FK_/p_`, PascalCase tables/columns; index/rollback discipline | SQL Server + EF Core 8 | High |
| `frontend-react-standards.md` | Frontend / React app | Only if a React UI is added (none present today) | Small focused functional components; typed props; no hardcoded strings; API calls via data layer; styling via CSS-in-JS, avoid `!important` | N/A currently | Conditional |
| `ui-standards.md` | Markup / styling / accessibility | Only if user-facing markup is added (none present today) | HTML5 doctype, semantic elements, alt text; low-specificity CSS; accessibility over ARIA | N/A currently | Conditional |

## Selection Rules

- Always apply `coding-standards.md`.
- For typical AlertService API work apply `backend-dotnet-standards.md`,
  `api-rest-standards.md`, and `service-architecture-standards.md`.
- Apply `database-standards.md` when touching EF Core, `AlertDbContext`, repositories,
  migrations, or the `database/` scripts.
- Apply `frontend-react-standards.md` / `ui-standards.md` only if a UI is introduced; the
  current solution is a backend API with no frontend.

## Starter Notes

- `/analyze-story` should select only the relevant subset for the story.
- `/implement-story` should rely on the selected subset recorded in the story cache instead of
  rereading all standards files.