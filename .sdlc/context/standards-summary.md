# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, handle exceptions deliberately, keep tests deterministic | No explicit versioning guidance | Global / highest |
| `backend-dotnet-standards.md` | Backend .NET code | Touching `.cs` or `.csproj` files | Follow C# naming/layout conventions, keep classes cohesive, avoid control-flow misuse of exceptions, preserve layered separation | Align with the solution's `.NET 8` target and current C# patterns | High when present |
| `api-rest-standards.md` | REST API contracts | Changing controllers, routes, request/response contracts, or OpenAPI/Swagger artifacts | Keep noun-based URIs, consistent status codes, JSON payloads, RFC7807-style errors, and deliberate versioning | Calls for OpenAPI `3.x`; current repo exposes Swagger at runtime but no checked-in contract file | High when present |
| `database-standards.md` | Database and persistence | Changing schema, migrations, repositories, SQL, or EF queries | Avoid `SELECT *`, keep data changes atomic, review indexing/performance, and preserve naming discipline | Validate against the repo's SQL Server + EF Core 8 stack | High when present |
| `service-architecture-standards.md` | Service boundaries and orchestration | Changing API/service/data boundaries, integrations, or startup wiring | Keep REST, business logic, and persistence separate; use DI correctly; reflect contract changes in Swagger; avoid leaking sensitive data | Validate against the repository's microservice-style ASP.NET Core service layout | High when present |
| `frontend-react-standards.md` | Frontend React/JS guidance | Touching `.tsx`, `.jsx`, `.ts`, or `.js` UI/client files | Keep components focused, avoid inline styles and direct API calls from presentation code, and keep tests behavior-focused | No current React app detected; keep for future client work | Conditional |
| `ui-standards.md` | HTML/CSS/accessibility guidance | Touching `.html`, `.css`, or `.scss` files | Prefer semantic HTML, accessible content, readable markup, and low-specificity modular CSS | No current standalone UI markup detected; keep for future UI assets | Conditional |

## Selection Rules

- Always apply `coding`.
- Add `backend-dotnet`, `api-rest`, `service-architecture`, and `database` for the current API/data stack as the touched slice requires.
- Add `frontend-react` or `ui` only when future work actually touches those file types.

## Standards Loading Model

- `standards\*.md` holds the full source of truth.
- `.github\instructions\standards\*.instructions.md` holds compact auto-applied rules.
- `.sdlc\context\standards-index.json` maps ids to the source file, compact instruction file, and applicable file globs.