# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): PascalCase types/members and `I`-prefixed interfaces; one primary class per file; DI composition in `Program.cs` and extension methods; global exception middleware for unhandled errors; async-first service/repository APIs with propagated `CancellationToken`; Serilog for structured logging.
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): Routes are unversioned under `api/alerts` (project convention; differs from company URL-version guidance), `[ApiController]` + data annotations drive validation and automatic 400 responses, controllers return typed `ActionResult` with explicit 200/201/204/404 mappings, and middleware returns `application/problem+json` with `traceId` for unhandled errors.
- Keep controllers and endpoints thin: validate input, call the service layer, map the response.
- Business logic in the service or domain layer; persistence and external calls through their own layers.
- Register dependencies in the existing composition root with the correct lifetime.
- Keep API contracts (DTOs) separate from persistence entities and external-vendor models.
- Async I/O end to end; never `.Result` or `.Wait()`; pass `CancellationToken` where the project does.
- Rethrow with `throw;`; no exceptions for normal control flow; return empty collections, not null.
- Use the shared `HttpClient` pattern (for example `IHttpClientFactory`); never one per request.
- Contract changes must be additive unless approved; keep OpenAPI or Swagger in sync when used.
- Never expose stack traces or internal details in responses.

## Data Access
- Project conventions (observed): EF Core repository pattern over `AlertDbContext`; SQL Server provider in runtime with in-memory/SQLite in tests; read paths use `AsNoTracking`; filtering/sorting/paging remain in repository query composition; migrations are maintained in `AlertService.Data.SQL/Migrations` with SQL script output in `database/`.
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Integrations
## Testing
- Project conventions (observed): xUnit with AAA organization, Moq for dependency mocking in API/service tests, repository tests use EF Core in-memory plus targeted relational SQLite checks, and test names follow `Method_Condition_Expected` readability.
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: `NOT_CONFIGURED` in this repository (company standard mentions 90% when adopted).

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
