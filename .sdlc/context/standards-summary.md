# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns; avoid hardcoded values; remove dead code/unused usings; validate nulls/bounds/failure paths explicitly; handle exceptions deliberately (no swallowing); don't log secrets; guard against SQL injection/XSS/token exposure; AAA-style deterministic tests, ≥90% coverage on changed code where enforced | None stated | Global / highest |
| `backend-dotnet-standards.md` | Backend / .NET code | Touching any C# code in `AlertService.*` projects | PascalCase for types/members, camelCase locals, `I`-prefixed interfaces; one class per file; braces always on new lines; use C# aliases (`int`/`string`); `throw;` not `throw ex;`; no exceptions for control flow; return empty collections not `null`; keep UI/business/data-access layered and don't call DB from controllers | Validate against .NET 8 (`net8.0` per `Directory.Build.props`) | High |
| `api-rest-standards.md` | API / external contract design | Changing `AlertsController` routes, request/response DTOs, or health endpoints | Nouns not verbs, hyphenated resource names; correct HTTP verb/status-code semantics (`201` create, `200`/`404`/`400` etc.); RFC7807 error payloads; URL path versioning (`v1`) recommended for breaking changes; version DTOs alongside controllers | Current API has no `v1` path segment and no committed OpenAPI file — flag any new breaking change per repo-profile gap | High |
| `service-architecture-standards.md` | Internal architecture | Changing controller/service/repository boundaries, DI, or async flows | Keep REST-facing classes separate from business logic; route persistence only through the service layer; no `Task.Wait()`/`Task.Result`; shared `HttpClient` instances; DI configured correctly; contract changes reflected in Swagger; unit tests required for changed behavior | Matches existing Controller→Service→`IAlertRepository` layering | High |
| `database-standards.md` | Database and persistence | Changing `AlertDbContext`, EF configurations, migrations, or repository queries | No `SELECT *`; set-based over cursors; short transactions; avoid unnecessary `ORDER BY`; scrutinize EF/LINQ-generated SQL; 3NF by default; naming: `IX_[Table]_[Columns]`, `PK_[Table]_[Column]`, `FK_[Column]_[RefTable]_[RefColumn]` | Validate against SQL Server / EF Core 8 | High |
| `frontend-react-standards.md` | Frontend / client app | N/A for this repository — no React/frontend project currently exists | Small functional components, prop contracts, no inline styles, approved data-fetching layer | Not applicable until a frontend project is added | Conditional / currently unused |
| `ui-standards.md` | Markup / styling / accessibility | N/A for this repository — no HTML/UI-rendering project currently exists | Semantic HTML5, accessible markup, low-specificity CSS | Not applicable until a UI-rendering project is added | Conditional / currently unused |

## Selection Rules

- Always apply the repository's general coding standards when present.
- Apply backend, API, service, database, frontend, or UI standards only when the story touches those areas and those standards exist in the target repository.
- For this repository, `frontend-react-standards.md` and `ui-standards.md` are cataloged but not currently applicable (no frontend/UI project exists); reconsider only if such a project is added.

## Starter Notes

- `/setup-repo-context` should confirm this summary against the bundled `standards/` folder and
  the target repository.
- `/analyze-story` should select only the relevant subset for the story.
- `/implement-story` should rely on the selected subset recorded in the story cache instead of
  rereading all standards files.