# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): PascalCase types/members, camelCase locals, `I`-prefixed interfaces; one class per file; `net8.0` with nullable + implicit usings; constructor DI; shared values in `AlertConstants`; structured Serilog logging with message templates; async end-to-end with `CancellationToken`.
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): attribute routing (`[ApiController]`, `[Route("api/alerts")]`); thin controllers returning `ActionResult<T>` with `ProducesResponseType`; DataAnnotations validation on DTOs (`[Required]`, `[StringLength]`, `[EnumDataType]`); `ExceptionHandlingMiddleware` maps unhandled errors to `ProblemDetails` 500; enums serialized as strings via `JsonStringEnumConverter`; no API versioning.
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
- Project conventions (observed): EF Core 8 on SQL Server; provider-agnostic `IAlertRepository` in `AlertService.Data`, EF impl in `AlertService.Data.SQL`; `IEntityTypeConfiguration` in `Configurations/`; `AsNoTracking` for reads; server-side filtering/paging/sorting; migrations via `dotnet-ef` (owned, regenerate `database/02_*.sql` idempotent script).
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Testing
- Project conventions (observed): xUnit `[Fact]`/`[Theory]`; Moq for mocking collaborators; `NullLogger<T>` and mocked `TimeProvider` for determinism; one test project per layer mirroring source folders; classes `<Type>Tests`, methods `Method_Scenario_ExpectedResult`; EF Core InMemory/Sqlite and `WebApplicationFactory<Program>` for data/health tests.
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: as recorded in `project-profile.md` section 7.

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
