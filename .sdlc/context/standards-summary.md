# Standards Summary

> Compact human-readable catalog. Routing (id → source file, compact instruction file,
> `applies_to` globs) lives in `manifest.json` → `standards.items`. `/setup-repo-context` confirms
> both against `standards/` and the target repository, keeping this table shape.

| Standard Id | Source File | Scope | Applies When | Key Rules | Priority |
|---|---|---|---|---|---|
| `coding` | `coding-standards.md` | Global engineering | Always | Match existing patterns, avoid hardcoded values, remove dead code/unused usings, validate nulls/failure paths, no swallowed exceptions or logged secrets, AAA-style focused tests, ≥90% coverage on new/changed code where applicable | Global / highest |
| `backend-dotnet` | `backend-dotnet-standards.md` | Backend / service code | Touching C# logic, APIs, middleware, or server-side tests | PascalCase/camelCase, `I` prefix, one primary class per file, braces on new lines, no exceptions for control flow, `throw;`, return empty collections not null, layered separation | High when present |
| `api-rest` | `api-rest-standards.md` | API / external contracts | Changing routes, request/response contracts, status codes or errors | Noun resources, correct HTTP verb semantics, JSON, RFC7807 errors, URL-path versioning, breaking change = major version, OpenAPI contract in source control | High when present |
| `service-architecture` | `service-architecture-standards.md` | Internal architecture | Changing boundaries, orchestration, DI, or integrations | Controllers separate from service logic, persistence only via service layer, API contracts separate from domain models, no blocking async, contract changes reflected in Swagger/OpenAPI | High when present |
| `database` | `database-standards.md` | Database / persistence | Changing schema, queries, migrations, repositories, or EF behavior | No `SELECT *`, set-based logic, consider indexing, treat EF/LINQ like SQL, naming `IX_`/`PK_`/`FK_`, DBA review and rollback plan for schema changes | High when present |
| `frontend-react` | `frontend-react-standards.md` | Frontend / client app | `NOT_APPLICABLE` — no frontend code in this repository | n/a | Not applicable |
| `ui` | `ui-standards.md` | Markup / styling / a11y | `NOT_APPLICABLE` — no HTML/CSS in this repository | n/a | Not applicable |

Version guidance: validate every row against the target repository's actual versions and
frameworks (this repository: .NET 8, EF Core 8.0.31); record `NOT_AVAILABLE` for standards that
do not exist there.

## Selection Rules

- Always apply `coding` when present.
- Apply other standards only when the work touches that area and the standard exists in the
  target repository.
- Record at most 5 ids in `work.json` → `selected_standards`.
- Typical mapping here: controllers/routes → `api-rest` + `backend-dotnet`; services/`Program.cs`
  → `service-architecture` + `backend-dotnet`; repositories/DbContext/migrations → `database`
  + `backend-dotnet`.

## Loading Model

1. `work.json` → `selected_standards`: the ids relevant to this work.
2. `.github/instructions/standards/*.instructions.md`: compact rules, auto-applied via `applyTo`.
3. `standards/*.md`: full source of truth, read only for an exact rule or missing detail.

Never reread the entire `standards/` folder by default.
