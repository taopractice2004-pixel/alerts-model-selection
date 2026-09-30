# Standards Summary

This file contains a compact, deterministic catalog of standards discovered under `standards/`.

- `api-rest-standards.md`: Contract-first OpenAPI guidance; resource/URI design; HTTP method semantics; standard error payloads (RFC7807-style); JSON responses; correlation IDs; .NET URL path versioning and backward-compatibility rules.
- `backend-dotnet-standards.md`: C#/.NET naming and file-layout rules; language usage (idiomatic C#), exception handling guidance, layered architecture and SOLID guidance.
- `coding-standards.md`: PR and review checklist; code-quality rules; testing expectations; security and reliability reminders; reviewer responsibilities.
- `database-standards.md`: DBA review expectations; query/procedure guidelines (avoid SELECT *, prefer set-based logic); modeling and naming conventions for tables, indexes, PKs/FKs.
- `frontend-react-standards.md`: React component and styling guidance; component sizing, state handling, test hooks, and code-review checklist for UI code.
- `service-architecture-standards.md`: Microservice design principles, operational guidance (containerization, CI/CD), layering for .NET APIs and BFFs, API review checklist.
- `ui-standards.md`: HTML/CSS accessibility and markup guidelines; semantic HTML, accessibility preferences, CSS conventions and minification guidance for production.
# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, validate failure paths, keep tests focused and deterministic | None stated until checked | Global / highest |
| `<backend standard file if present>` | Backend / service code | Touching backend logic, APIs, jobs, middleware, or server-side tests | Preserve layering, naming/layout conventions, and operational safety practices | Validate against target repository versions and frameworks | High when present |
| `<api standard file if present>` | API / external contract design | Changing routes, request/response contracts, events, or externally consumed interfaces | Keep contracts consistent and version changes deliberate | Validate protocol/versioning guidance against target repository | High when present |
| `<service architecture standard file if present>` | Internal architecture | Changing boundaries, orchestration, async flows, or integrations | Keep clear boundaries between delivery, business, and persistence/integration layers | Validate architecture-specific guidance in context | High when present |
| `<database standard file if present>` | Database and persistence | Changing schema, queries, migrations, repositories, or ORM behavior | Avoid risky query patterns, review indexing/performance, preserve naming and rollback discipline | Validate against target repository database technology | High when present |
| `<frontend standard file if present>` | Frontend / client app | Touching UI components, state, routing, or client-side data flows | Keep components focused, separate concerns, preserve accessibility and maintainability | Validate against target repository UI stack | Conditional |
| `<ui standard file if present>` | Markup / styling / accessibility | Touching markup, styling, layout, or user-facing presentation | Prefer semantic structure, accessible content, and maintainable styling | Validate against target repository rendering technology | Conditional |

## Selection Rules

- Always apply the repository's general coding standards when present.
- Apply backend, API, service, database, frontend, or UI standards only when the story touches those areas and those standards exist in the target repository.

## Starter Notes

- `/setup-repo-context` should confirm this summary against the bundled `standards/` folder and
  the target repository.
- `/analyze-story` should select only the relevant subset for the story.
- `/implement-story` should rely on the selected subset recorded in the story cache instead of
  rereading all standards files.