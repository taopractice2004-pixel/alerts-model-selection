# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): one primary type per file; file-scoped namespaces matching the folder (`AlertService.<Project>.<Folder>`); constructor injection into `private readonly _field`; async methods end in `Async` and take `CancellationToken cancellationToken = default` last; shared values in `AlertConstants` (`AlertService.Common`); `TimeProvider` injected instead of `DateTime.UtcNow`; mapping through extension methods in `AlertService.API/Mappings/`; structured `ILogger` templates (`"Alert {AlertId} not found"`); `ArgumentNullException.ThrowIfNull` for public inputs; DI registered in `Program.cs` or the project's `ServiceCollectionExtensions`; project-specific extension methods for host wiring.
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): controllers use `[ApiController]`, route `api/alerts`, `[Produces("application/json")]`, `[ProducesResponseType]` per status, and an XML `<summary>` per action; routes are unversioned (`api/<resource>`, kebab-case segments); services return `null` or `bool` for not found and controllers map that to `NotFound()`/`NoContent()`; create returns `CreatedAtRoute`; request validation is DataAnnotations on DTOs (`Required`, `StringLength`, `RegularExpression`, `EnumDataType`) with limits from `AlertConstants`; list endpoints return `PagedResponse<T>`; enums serialize as strings (`JsonStringEnumConverter`); unhandled exceptions return RFC 7807 `ProblemDetails` with `traceId` through `ExceptionHandlingMiddleware`; Swagger enabled in Development; health probes at `/health/live` and `/health/ready`.
  Conflict noted: company standards require URL-path versioning and an OpenAPI YAML contract kept in source control; this project is unversioned with Swagger-generated docs only. Follow the project.
- Keep controllers and endpoints thin: validate input, call the service layer, map the response.
- Business logic in the service or domain layer; persistence and external calls through their own layers.
- Register dependencies in the existing composition root with the correct lifetime.
- Keep API contracts (DTOs) separate from persistence entities and external-vendor models.
- Async I/O end to end; never `.Result` or `.Wait()`; pass `CancellationToken` where the project does.
- Rethrow with `throw;`; no exceptions for normal control flow; return empty collections, not null.
- Contract changes must be additive unless approved; keep OpenAPI or Swagger in sync when used.
- Never expose stack traces or internal details in responses.

## Data Access
- Project conventions (observed): EF Core with SQL Server; entity mapping in `IEntityTypeConfiguration<T>` classes (`AlertService.Data.SQL/Configurations/`); repository behind an interface in `AlertService.Data` implemented in `AlertService.Data.SQL/Repositories/`; read queries use `AsNoTracking`; filtering, sorting, and paging done in the database; enums stored as strings; dates stored UTC (`datetime2`); migrations via `dotnet ef` into `AlertService.Data.SQL/Migrations/`, with the idempotent script regenerated into `database/02_AlertServiceDb_Migrations.sql`; retry on failure enabled for SQL Server.
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Testing
- Project conventions (observed): xUnit `[Fact]` tests with `Assert`; Moq for `IAlertRepository` and `IAlertService`; fixed `FixedNow` via a mocked `TimeProvider`; `NullLogger<T>.Instance`; repository tests use a unique EF InMemory database per test class instance; web host tests use `WebApplicationFactory<Program>` with Sqlite; test names `Method_Behavior`; see `project-profile.md` section 7.
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: as recorded in `project-profile.md` section 7.

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
