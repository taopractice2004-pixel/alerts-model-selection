# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Follow existing project patterns, avoid hardcoded values, validate failure paths, and keep tests deterministic and focused | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend .NET code | Touching C# services, APIs, middleware, repositories, or tests | Enforce .NET naming/layout conventions, cohesive methods/classes, layered boundaries, and safe exception/resource handling | High when present |
| `api-rest` | `api-rest-standards.md` | API contracts | Changing controller routes, methods, request/response shapes, or versioning behavior | Keep noun-based URIs, consistent HTTP semantics/status codes, standardized JSON errors, and explicit versioning/backward compatibility rules | High when present |
| `service-architecture` | `service-architecture-standards.md` | Service boundaries and orchestration | Changing service boundaries, composition roots, external clients, or request-processing flows | Preserve controller-service-repository separation, isolate migration/reconciliation flows, use DI and async correctly, avoid sensitive data exposure | High when present |
| `database` | `database-standards.md` | Database and persistence | Changing SQL scripts, EF queries, DbContext/repository code, or migrations | Avoid `SELECT *`, keep transactional/query efficiency, follow naming/index conventions, and coordinate schema/performance/rollback concerns | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend React | Touching React components/state/styling/client API interactions | Keep components focused, maintain separation of concerns, avoid inline/hardcoded UI patterns, and keep UI tests maintainable | Conditional |
| `ui` | `ui-standards.md` | HTML/CSS/accessibility | Touching HTML/CSS/SCSS markup or styling | Use semantic HTML, accessible content, low-specificity maintainable CSS, and consistent formatting | Conditional |

Version guidance: standards files are present and active in this repository; apply per touched
scope and selected ids in `work.json`.

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
