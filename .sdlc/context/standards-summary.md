# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| coding-standards.md | Global engineering | Every change and review | Match patterns; avoid hardcoded values and dead code; validate failure paths; handle exceptions/resources; keep tests deterministic | No version guidance stated | Global / highest |
| backend-dotnet-standards.md | C#/.NET implementation | Backend, API, middleware, or server tests | PascalCase public names, focused classes/methods, layered architecture, explicit failure handling, async without blocking, DI | Apply to .NET 8 code in this repository | High |
| api-rest-standards.md | REST contracts | Routes, DTOs, status codes, or external API behavior | OpenAPI 3.x, noun-based resources, consistent HTTP semantics, JSON/RFC7807-style errors, deliberate versioning and breaking changes | Validate contract versioning against the current API | High |
| service-architecture-standards.md | Service boundaries and operations | Layering, orchestration, integrations, or deployment concerns | Keep service focused, separate controllers/business/data, use DI and async correctly, preserve contract/domain boundaries, avoid sensitive logging | Apply to the .NET microservice architecture | High |
| database-standards.md | SQL/EF persistence | Schema, migrations, queries, repositories, or ORM behavior | Review indexing/performance, use set-based/parameterized access, preserve normalization and naming, plan rollback for production changes | Apply to SQL Server and EF Core changes | High |
| frontend-react-standards.md | React frontend | Only if a React client is added or changed | Focused components, separated data/loading concerns, typed props, consistent styling, behavior-focused tests | Not applicable to current backend-only solution | Conditional |
| ui-standards.md | HTML/CSS/accessibility | Only if user-facing markup/styles are added or changed | Semantic HTML, metadata/alt text, accessible controls, low-specificity CSS, consistent formatting | Not applicable to current backend-only solution | Conditional |

## Selection Rules

- Always apply `coding-standards.md`.
- Apply backend, API, service, and database standards for typical AlertService work.
- Apply frontend and UI standards only if a client or presentation layer is introduced.