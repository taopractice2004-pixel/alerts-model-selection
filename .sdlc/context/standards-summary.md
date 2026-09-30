# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, handle exceptions deliberately, keep tests focused and deterministic | No explicit version guidance | Global / highest |
| `backend-dotnet-standards.md` | Backend .NET code | Touching API, middleware, services, repositories, or backend tests | PascalCase and `I`-prefixed interfaces, one primary class per file, braces on new lines, small cohesive methods, keep persistence out of UI code | Repository targets .NET 8; standard itself gives no version-specific rule | High |
| `api-rest-standards.md` | REST API contracts | Changing routes, request/response models, status codes, or externally consumed behavior | Resource nouns, JSON payloads, RFC7807-style errors, clear status-code use, treat breaking changes as versioned contract changes | OpenAPI 3.x expected; URL path versioning such as `v1`/`v1.2`/`v1.2.3` | High |
| `service-architecture-standards.md` | Service boundaries and orchestration | Changing controller-service-repository interactions, DI, async flows, external clients, or service tests | Keep REST classes separate from business logic, route persistence through services, avoid blocking async calls, reflect contract changes in Swagger, avoid sensitive logging | No explicit version guidance | High |
| `database-standards.md` | Database and persistence | Changing EF models, migrations, SQL, repositories, indexes, or deployment scripts | Review schema changes, consider indexing and execution plans, scrutinize EF/LINQ like handwritten SQL, keep naming conventions consistent, prepare rollback approach for production-impacting changes | SQL Server-oriented naming guidance; no explicit engine version rule | High |
| `frontend-react-standards.md` | Frontend React code | Touching a React UI, client state, or frontend styling in this or a future companion app | Small focused functional components, typed props/contracts, avoid hardcoded UI strings, separate presentation from data loading, clean up listeners/subscriptions | React-focused; no explicit version rule | Conditional |
| `ui-standards.md` | HTML/CSS and accessibility | Touching markup, layout, styling, or user-facing presentation | Use semantic HTML5, meaningful metadata and alt text, accessible structure, low-specificity CSS, avoid presentational tags and `!important` | HTML5 guidance; no explicit CSS/HTML version rule beyond HTML5 | Conditional |

## Selection Rules

- Always apply `coding-standards.md`.
- Apply backend, API, service, and database standards for this repository's typical API/data work.
- Apply frontend and UI standards only when a story introduces or changes user-facing client code.
