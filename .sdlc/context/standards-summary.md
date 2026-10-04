# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Follow existing patterns; avoid hardcoded values; keep error handling explicit; protect sensitive data; keep tests deterministic | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / .NET implementation | Touching `.cs` or `.csproj` files | Consistent C# naming/layout; cohesive methods/classes; avoid control-flow exceptions; keep layered boundaries | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Touching controllers or REST contracts | Resource-oriented URIs; correct HTTP semantics/status codes; JSON + RFC7807-style errors; deliberate versioning and backward compatibility | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal service boundaries | Touching service orchestration, dependency boundaries, or composition root | Keep controller/service/data boundaries clear; route persistence through service/repository layers; avoid leaking internal models/contracts | High when present |
| `database` | `database-standards.md` | Database / persistence | Touching SQL, migrations, repositories, DbContext | Avoid `SELECT *`; use scoped transactions carefully; review index/performance impact; keep naming conventions consistent | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | Touching React/TypeScript/JavaScript UI code | Small focused components, clear state/event handling, maintainable styling and tests | Not currently applicable in this repository |
| `ui` | `ui-standards.md` | HTML/CSS styling and accessibility | Touching HTML/CSS/SCSS | Semantic markup, accessibility-first structure, low-specificity maintainable CSS | Not currently applicable in this repository |

Version guidance: validate every row against the target repository's actual technologies and
files; record `NOT_AVAILABLE` when a standard source file is missing.

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
