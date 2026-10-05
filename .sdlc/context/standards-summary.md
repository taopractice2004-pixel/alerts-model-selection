# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Stay aligned with existing patterns, avoid hardcoded values, validate edge/failure paths, and keep tests focused and deterministic | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Touching C# projects, controllers, services, middleware, or tests | Consistent naming/layout, small cohesive methods/classes, deliberate exception handling, and layered separation | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing controllers, routes, request/response models, or external API behavior | Resource-oriented URIs, consistent HTTP semantics/status codes, RFC7807-style errors, and deliberate versioning | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing orchestration, dependency boundaries, integrations, or DI composition roots | Keep REST delivery separate from business logic, route persistence through services, and reflect contract changes in Swagger/OpenAPI output | High when present |
| `database` | `database-standards.md` | Database / persistence | Changing repositories, EF queries, migrations, schema, or SQL scripts | Review indexing/performance, keep names consistent, avoid broad projections, and treat EF-generated SQL with DBA-level scrutiny | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | Touching `*.ts`, `*.tsx`, `*.js`, or `*.jsx` client code if added later | Small focused components, approved data-loading patterns, stable contracts/test hooks, and maintainable styling | Conditional |
| `ui` | `ui-standards.md` | Markup / styling / a11y | Touching HTML/CSS/SCSS if added later | Semantic HTML, accessibility-first content structure, and low-specificity maintainable CSS | Conditional |

Version guidance: all seven standards source files are present in this repository. The active
implementation today is backend-focused (`.cs`, `.csproj`, SQL, controllers, services, and EF
Core); frontend/UI standards remain available for future matching files.

## Selection Rules

- Always apply `coding` when present.
- Apply other standards only when the work touches that area and the standard exists in the
  target repository.
- Record at most 5 ids in `work.json` → `selected_standards`.

## Loading Model

1. `work.json` → `selected_standards`: the ids relevant to this work.
2. `.github/instructions/standards/*.instructions.md`: compact rules, auto-applied via `applyTo`.
3. `standards/*.md`: full source of truth, read only for an exact rule or missing detail.

Never reread the entire `standards/` folder by default.
