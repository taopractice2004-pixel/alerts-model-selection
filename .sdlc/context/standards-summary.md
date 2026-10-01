# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `standards/coding-standards.md` | Global coding quality | Any code change | Follow existing patterns, avoid hardcoded values, validate failure paths, keep tests deterministic, avoid dead/suppressed code | Technology-agnostic; no explicit version constraints | Global |
| `standards/backend-dotnet-standards.md` | C#/.NET backend | `.cs` and `.csproj` changes | Enforce naming/layout conventions, layered design, cohesive classes, deliberate exception/resource handling | Targets .NET/C# backend practices; align with current SDK/framework in repo | High when present |
| `standards/api-rest-standards.md` | REST API contracts | Controller routes, API behavior, or OpenAPI-related changes | Contract-first OpenAPI, resource-oriented URIs, method semantics, consistent status/errors, versioning discipline | OpenAPI 3.x and URL versioning guidance | High when present |
| `standards/database-standards.md` | SQL/persistence | SQL, migrations, DbContext/repository/query changes | No `SELECT *`, set-based queries, transaction discipline, indexing awareness, naming conventions, rollback readiness | Database-agnostic with SQL Server/EF scrutiny expectations | High when present |
| `standards/service-architecture-standards.md` | Service boundaries/architecture | Service layer, DI wiring, API orchestration, external client boundaries | Keep API/service/data boundaries clean, isolate persistence through service layer, safe async and dependency use | Microservice-oriented guidance; no explicit framework version pin | High when present |
| `standards/frontend-react-standards.md` | React/frontend | `.tsx/.jsx/.ts/.js` UI/client code changes | Small focused components, clear prop/state contracts, teardown cleanup, styling discipline, behavior-focused tests | React ecosystem guidance; use project conventions where applicable | Conditional |
| `standards/ui-standards.md` | HTML/CSS accessibility and structure | Markup/styling changes | Semantic HTML, accessibility-first structure, maintainable CSS selectors/formatting | HTML5/CSS best-practice guidance | Conditional |

## Selection Rules

- Always apply `coding`.
- Select additional standards by touched surface (`backend-dotnet`, `api-rest`, `database`, `service-architecture`, `frontend-react`, `ui`).
- Keep selected standards compact and scoped in `implementation-cache.json:selected_standards`.