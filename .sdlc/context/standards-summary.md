# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): `PascalCase types and members with folder-aligned namespaces; one primary type per file; DI is centralized in Program.cs and ServiceCollectionExtensions; nullable and implicit usings are enabled solution-wide; async flows from controllers through repositories with CancellationToken parameters; Serilog handles host/request logging and ILogger is used inside services and middleware.`
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): `Controllers use attribute routing under /api/alerts without URL versioning, return typed IActionResult/ActionResult responses, and rely on [ApiController] plus DataAnnotations for 400 validation behavior; enums are serialized as strings; unhandled exceptions are converted to RFC7807-style ProblemDetails with a traceId by middleware; health probes are exposed at /health/live and /health/ready.`
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
- Project conventions (observed): `Persistence uses EF Core with a repository interface in AlertService.Data and SQL Server implementation in AlertService.Data.SQL; read queries use AsNoTracking where appropriate; migrations live in AlertService.Data.SQL/Migrations and the generated deployment script lives in database/02_AlertServiceDb_Migrations.sql; DbContext registration enables SQL retry-on-failure and migration application is an explicit startup option.`
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Testing
- Project conventions (observed): `Tests live in dedicated *.Tests projects, use xUnit, follow Arrange-Act-Assert, use Moq for service/controller collaborators, and use EF Core InMemory or SQLite plus WebApplicationFactory only where a behavior needs repository or HTTP-level coverage.`
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: as recorded in `project-profile.md` section 7.

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
