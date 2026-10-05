# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate null/failure paths, handle exceptions deliberately, and keep tests focused/deterministic | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Touching any C# production or test code in this solution | Use standard C# naming/layout, keep methods cohesive, preserve layering, and avoid hardcoded values or exception-driven control flow | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing `AlertsController`, request/response DTOs, routes, status codes, or any future checked-in OpenAPI artifact | Keep resource-oriented URIs, consistent HTTP semantics, JSON contracts, RFC7807-style errors, and deliberate versioning | High when present |
| `database` | `database-standards.md` | Database / persistence | Changing SQL scripts, EF migrations, `AlertDbContext`, repositories, or query behavior | Coordinate schema risk, prefer efficient set-based access, review indexing/performance, and keep naming/rollback discipline | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing `Program.cs`, service classes, dependency wiring, or integration boundaries | Keep controllers separate from business logic, route persistence through services/repositories, configure DI correctly, and reflect contract changes in Swagger/OpenAPI output | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | Only if this repository later gains React or other TS/JS UI code | Keep components focused, separate state from presentation, avoid hardcoded UI strings, and follow project data/styling patterns | Conditional / not currently applicable |
| `ui` | `ui-standards.md` | Markup / styling / a11y | Only if this repository later adds HTML/CSS/SCSS assets | Favor semantic HTML, accessible content, low-specificity CSS, and maintainable presentation structure | Conditional / not currently applicable |

Version guidance: validate every row against the target repository's actual versions and
frameworks; record `NOT_AVAILABLE` for standards that do not exist there.

## Selection Rules

- Always apply `coding` when present.
- For current AlertService work, the most likely additional standards are `backend-dotnet`, `api-rest`, `database`, and `service-architecture`; select only the ids that match the touched slice.
- `frontend-react` and `ui` exist in the repository standards set but are not expected for the current API-only codebase unless new frontend assets are introduced.
- Record at most 5 ids in `work.json` → `selected_standards`.

## Loading Model

1. `work.json` → `selected_standards`: the ids relevant to this work.
2. `.github/instructions/standards/*.instructions.md`: compact rules, auto-applied via `applyTo`.
3. `standards/*.md`: full source of truth, read only for an exact rule or missing detail.

Never reread the entire `standards/` folder by default.
