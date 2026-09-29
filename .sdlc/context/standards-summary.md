# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate nulls/bounds/failure paths, keep tests deterministic, remove dead code and unused imports | No version guidance stated | Global / highest |
| `backend-dotnet-standards.md` | Backend .NET code | Touching C# services, controllers, middleware, libraries, or tests | PascalCase and `I`-prefixed interfaces, braces on new lines, one primary class per file, small cohesive methods/classes, layered separation between UI/business/data | Align to repository target `net8.0` and existing C# conventions | High |
| `api-rest-standards.md` | REST API contracts | Changing routes, request/response contracts, versioning, or externally consumed errors | Contract-first OpenAPI expectation, noun-based URIs, consistent HTTP verb semantics, JSON responses, RFC7807-style errors, deliberate versioning and compatibility handling | Use URL path versioning when versioning is introduced or changed | High |
| `service-architecture-standards.md` | Service boundaries and microservice design | Changing orchestration, service boundaries, integrations, async flows, or DI wiring | Keep controllers separate from service logic, route persistence through service layer, use dedicated external clients, avoid blocking async code, reflect contract changes in Swagger/OpenAPI | No version guidance stated | High |
| `database-standards.md` | Database and persistence | Changing schema, EF queries, migrations, repositories, or SQL scripts | Review rollback/indexing/performance, avoid `SELECT *`, prefer set-based logic, scrutinize EF/LINQ queries like handwritten SQL, follow naming conventions for DB objects | Coordinate with SQL Server and EF Core conventions already used in repo | High |
| `frontend-react-standards.md` | Frontend React code | Touching any future client-side React components or tests | Small focused components, project-consistent state/data-fetching, no hardcoded UI strings, avoid inline styles except dynamic values, clean teardown, behavior-focused tests | Apply only if React surface is added or modified | Conditional |
| `ui-standards.md` | Markup, styling, accessibility | Touching any future HTML/CSS or user-facing markup | Semantic HTML, explicit metadata, meaningful alt text, low-specificity CSS, accessible content, avoid deprecated/presentational markup | Apply only when UI markup or styles are part of the change | Conditional |

## Selection Rules

- Always apply `coding-standards.md`.
- Add backend, API, service, and database standards for server-side work in this repository.
- Add frontend or UI standards only if a story introduces or changes a client-facing surface.