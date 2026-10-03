# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): file-scoped namespaces matching the folder path; one primary type per file; PascalCase types and members, `_camelCase` private readonly fields; constructor injection; interfaces prefixed `I`; shared limits, sort keys, and regex patterns live in `AlertService.Common/Constants/AlertConstants.cs`; `async`/`await` with `CancellationToken` (default `= default` in services and repositories); `ArgumentNullException.ThrowIfNull` on public service inputs; structured logging via `ILogger<T>` message templates (Serilog); time via injected `TimeProvider`, never `DateTime.UtcNow`; one-line `///` summaries on public controller actions and extensions.
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting. Always use braces on conditionals and loops.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): routes `api/alerts` (no URL versioning; company standard asks for `/v1`, project convention is followed and new routes stay consistent); controllers use `[ApiController]`, `[Route]`, `[Produces("application/json")]`, and `ProducesResponseType` per status; validation is DataAnnotations on DTOs (auto 400 via `[ApiController]`); "not found" is `null`/`false` from the service mapped to `NotFound()`; `POST` returns `CreatedAtRoute`; unhandled errors become RFC 7807 `ProblemDetails` (500, `traceId`) from `ExceptionHandlingMiddleware`; enums are strings in JSON (`JsonStringEnumConverter`); entity <-> DTO mapping is extension methods in `AlertService.API/Mappings/`; services are registered scoped in `Program.cs`; list endpoints return `PagedResponse<T>`.
- Keep controllers and endpoints thin: validate input, call the service layer, map the response.
- Business logic in the service or domain layer; persistence and external calls through their own layers.
- Register dependencies in the existing composition root with the correct lifetime.
- Keep API contracts (DTOs) separate from persistence entities and external-vendor models.
- Async I/O end to end; never `.Result` or `.Wait()`; pass `CancellationToken` where the project does.
- Rethrow with `throw;`; no exceptions for normal control flow; return empty collections, not null.
- Contract changes must be additive unless approved; keep OpenAPI or Swagger in sync when used.
- Resource names are nouns (hyphenated if multi-word); HTTP verbs express the action; errors use RFC 7807 `ProblemDetails` with a correlation id.
- Never expose stack traces or internal details in responses.

## Data Access
- Project conventions (observed): EF Core 8 code-first with SQL Server; repository per aggregate behind an interface in `AlertService.Data/Interfaces/`, implementation in `AlertService.Data.SQL/Repositories/`; only repositories touch `AlertDbContext`; entity mapping via `IEntityTypeConfiguration<T>` in `Configurations/`; `AsNoTracking` on read queries; `Severity` stored as string; dates stored UTC `datetime2`; migrations via `dotnet ef` into `AlertService.Data.SQL/Migrations/`, and the idempotent script `database/02_AlertServiceDb_Migrations.sql` is regenerated from them.
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Testing
- Project conventions (observed): xUnit `[Fact]` tests; Moq for dependencies; xUnit `Assert`; fixed `DateTimeOffset` and mocked `TimeProvider` for time; `NullLogger<T>` for loggers; EF Core InMemory per-test database (`Guid` name) for repository tests; `WebApplicationFactory<Program>` with SQLite for HTTP-level tests (see `project-profile.md` section 7).
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: as recorded in `project-profile.md` section 7.

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
