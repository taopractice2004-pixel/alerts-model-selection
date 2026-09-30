# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Follow existing patterns, avoid hardcoding, remove dead code, validate error paths/nulls, no swallowed exceptions, protect sensitive data, deterministic tests | None stated | Highest |
| `backend-dotnet-standards.md` | C#/.NET backend | Any C# source/test change | PascalCase/camelCase conventions, interface `I` prefix, one primary class per file, braces/new-line style, `throw;` rethrow, short cohesive methods, layered architecture | Align with repository `net8.0` baseline | High |
| `api-rest-standards.md` | REST contract | Route/DTO/endpoint changes | OpenAPI 3.x contract-first, noun-based resource URIs, proper HTTP status/method semantics, RFC7807-style errors, URL path versioning | Version API on breaking changes | High |
| `service-architecture-standards.md` | Service boundaries | Controller/service/repository changes | Keep transport separate from business logic, route persistence through service/repository, avoid blocking async, enforce DI discipline, test changed behavior | Keep layering consistent with current architecture | High |
| `database-standards.md` | Data layer | EF, SQL, migration, schema changes | No `SELECT *`, favor set-based operations, controlled transactions, review query performance/indexing, naming rules for PK/FK/IX | Coordinate breaking DDL with rollout/rollback planning | High |
| `frontend-react-standards.md` | React frontend | Frontend changes only | Component/state/styling/testing conventions | Not applicable to current repo content | Conditional |
| `ui-standards.md` | HTML/CSS/UI | UI markup/style changes only | Semantic HTML, accessibility, CSS naming/structure consistency | Not applicable to current repo content | Conditional |

## Selection Rules

- Always apply `coding-standards.md`.
- Apply additional standards only for touched layers.
- Current repository has no frontend project; `frontend-react-standards.md` and `ui-standards.md` are typically not applicable.
