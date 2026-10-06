# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Follow existing patterns, avoid hardcoding, validate null/bounds/failure paths, and keep tests deterministic | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Editing C# production or test projects | Enforce naming/layout conventions, cohesive classes/methods, explicit exception handling, and layered separation | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing controllers, routes, payload contracts, or API versioning | Resource-noun URI design, method semantics, RFC7807-style error consistency, and deliberate version compatibility | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing service boundaries, orchestration, or external integrations | Preserve API/service/persistence boundaries, DI correctness, async correctness, and contract-awareness | High when present |
| `database` | `database-standards.md` | Database / persistence | Touching SQL, migrations, repositories, or EF query behavior | Select required columns only, prefer set-based operations, maintain naming conventions, and plan rollback/index impact | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | Editing TS/JS UI components or client-side state/data flows | Small focused components, clear props/contracts, separated concerns, and maintainable styling/testing patterns | Conditional |
| `ui` | `ui-standards.md` | Markup / styling / a11y | Editing HTML/CSS/SCSS | Semantic HTML5 structure, accessibility-first markup, and low-specificity maintainable CSS | Conditional |

Version guidance: validate every row against the target repository's actual versions and
frameworks; record `NOT_AVAILABLE` for standards that do not exist there.

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
