# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, keep tests focused and deterministic | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Touching backend logic, APIs, jobs, middleware, or server-side tests | Preserve layering, naming/layout conventions, and operational safety practices | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing routes, request/response contracts, events, or externally consumed interfaces | Keep contracts consistent; version changes deliberately | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing boundaries, orchestration, async flows, or integrations | Clear boundaries between delivery, business, and persistence/integration layers | High when present |
| `database` | `database-standards.md` | Database / persistence | Changing schema, queries, migrations, repositories, or ORM behavior | Avoid risky query patterns, review indexing, preserve naming and rollback discipline | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | `NOT_APPLICABLE` — no React/JS/TS frontend in this repo | Focused components, separated concerns, accessibility, maintainability | Not applicable |
| `ui` | `ui-standards.md` | Markup / styling / a11y | `NOT_APPLICABLE` — no HTML/CSS/SCSS in this repo | Semantic structure, accessible content, maintainable styling | Not applicable |

Version guidance: validated against this repository — stack is .NET 8 / C# backend only.
`frontend-react` and `ui` are `NOT_APPLICABLE` here; the other five standards apply.

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
