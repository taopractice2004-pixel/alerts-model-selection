# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Stay aligned with existing patterns, avoid hardcoded values, validate nulls and failure paths, and keep changes reviewable and secure. | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Touching C# source, project files, middleware, services, or tests | Use standard .NET naming, cohesive classes, deliberate exception handling, and clear separation of UI, business, and data access concerns. | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing controllers, routes, request/response payloads, or API documentation | Prefer contract-first OpenAPI, noun-based resources, consistent status codes, RFC7807-style errors, and deliberate versioning for breaking changes. | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing service boundaries, orchestration, DI, integrations, or application startup | Keep controllers separate from business logic, route persistence through services, isolate migrations/reconciliation flows, and keep contracts distinct from domain models. | High when present |
| `database` | `database-standards.md` | Database / persistence | Changing EF Core mappings, repositories, migrations, SQL scripts, or query behavior | Review performance and rollback impact, avoid broad queries, keep naming disciplined, and treat ORM-generated SQL with the same scrutiny as handwritten SQL. | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | Only if React or other JS/TS UI code is added or modified | Favor small focused components, clear state/event handling, approved data-fetching patterns, and maintainable styling. | Conditional / currently inactive |
| `ui` | `ui-standards.md` | Markup / styling / a11y | Only if HTML/CSS/SCSS assets are added or modified | Prefer semantic HTML, accessible content, readable structure, and low-specificity maintainable CSS. | Conditional / currently inactive |

Version guidance: validate every row against the target repository's actual versions and
frameworks; record `NOT_AVAILABLE` for standards that do not exist there.

## Selection Rules

- Always apply `coding` when present.
- For this repository, `backend-dotnet`, `api-rest`, `service-architecture`, and `database` are the most likely companions for production work.
- Use `frontend-react` and `ui` only if the repository gains matching source files.
- Record at most 5 ids in `work.json` → `selected_standards`.

## Loading Model

1. `work.json` → `selected_standards`: the ids relevant to this work.
2. `.github/instructions/standards/*.instructions.md`: compact rules, auto-applied via `applyTo`.
3. `standards/*.md`: full source of truth, read only for an exact rule or missing detail.

Never reread the entire `standards/` folder by default.
