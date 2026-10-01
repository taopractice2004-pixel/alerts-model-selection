# Standards Summary

| Standard File | Scope | Applies When | Key Rules | Version Guidance | Priority |
|---|---|---|---|---|---|
| `coding-standards.md` | Global engineering | Always | Match existing patterns; avoid hardcoded values; validate nulls/bounds/failure paths; handle exceptions deliberately; dispose resources; no secrets in logs; deterministic AAA tests; ≥90% coverage for changed code when the rule applies | None stated | Global / highest |
| `backend-dotnet-standards.md` | Backend / .NET code | Touching `**/*.cs`, `**/*.csproj` | PascalCase types/members, camelCase locals, `I`-prefixed interfaces; one class/namespace per file; braces on new lines; `throw;` not `throw ex;`; short cohesive methods; return empty collections not null; layered UI/business/data separation | Target net8.0 / C# 12 | High (present, used) |
| `api-rest-standards.md` | API / external contract | Touching `**/*Controller.cs`, `openapi/swagger*` | Resource nouns, predictable URIs, correct HTTP verbs/status codes, JSON, RFC7807 errors, deliberate versioning (URL path), keep contract in source control | Validate vs .NET versioning | High (present, used by `AlertsController`) |
| `service-architecture-standards.md` | Internal architecture | Touching `**/*Service.cs`, `**/*Client.cs`, `Program.cs` | Single-responsibility services; REST-facing classes separate from service logic; persistence via service layer; correct async (no `.Result`/`.Wait()`); DI configured correctly; shared `HttpClient`; no secrets leaked | Validate in context | High (present, used) |
| `database-standards.md` | Database / persistence | Touching `**/*.sql`, `Migrations/**`, `*DbContext.cs`, `*Repository.cs` | Select only needed columns (no `SELECT *`); set-based logic; careful transactions; scrutinize EF/LINQ; 3NF default; PascalCase names; `IX_/PK_/FK_` index/key naming; rollback discipline | Validate vs SQL Server / EF 8 | High (present, used) |
| `frontend-react-standards.md` | Frontend / client app | Touching `**/*.tsx,jsx,ts,js` | Small focused components; functional components; prop contracts; cleanup listeners; separate presentational vs container; avoid inline styles | Validate vs UI stack if added | Conditional (no frontend code yet) |
| `ui-standards.md` | Markup / styling / a11y | Touching `**/*.html,css,scss` | HTML5 doctype/lang/encoding; semantic elements; `alt` text; low-specificity hyphenated CSS classes; accessibility over ARIA | Validate vs rendering tech if added | Conditional (no UI markup yet) |

## Selection Rules

- Always apply the repository's general coding standards (`coding`).
- This repository actively uses: `coding`, `backend-dotnet`, `api-rest`, `service-architecture`,
  `database`. Apply `frontend-react` and `ui` only if/when frontend or markup is added.

## Standards Loading Model

Standards now use a three-tier split to minimize repeated context:

- `standards/*.md` — full source of truth. Read a full file only when an exact rule or missing
  detail is required.
- `.github/instructions/standards/*.instructions.md` — compact, AI-enforceable rules that
  Copilot auto-applies via `applyTo` globs for matching files. These never duplicate the full
  document.
- `.sdlc/context/standards-index.json` — routing index mapping each standard `id` to its source
  file, compact instruction file, and applicable file types.

Stages record the relevant standard ids in `implementation-cache.json` under
`selected_standards` (for example `["coding", "backend-dotnet", "api-rest"]`). Downstream
stages rely on the cache plus the auto-applied compact instruction files and do not reread the
entire `standards/` folder.

## Starter Notes

- `/setup-repo-context` should confirm this summary and `standards-index.json` against the
  bundled `standards/` folder and the target repository.
- `/analyze-story` should select only the relevant standard ids (from `standards-index.json`)
  into `implementation-cache.json`.
- `/implement-story`, `/fix-bugs`, and `/unit-testing` should rely on the selected ids and the
  auto-applied compact instruction files, reading a full `standards/*.md` file only for an exact
  rule or missing detail.