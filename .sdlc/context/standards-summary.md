# Standards Summary

<!-- The condensed, actionable coding rules for THIS repository. The only place rules live:
     path-specific instruction files point here instead of repeating them.
     Built by /setup-repo-context from (1) conventions observed in the code and (2) the company
     standards in standards/, condensed to actionable lines. Never paste whole standards documents.
     If an observed project convention conflicts with a company rule, follow the project and note it.
     Setup removes sections for areas the repository does not have. -->

## General
- Project conventions (observed): PascalCase types/members, camelCase locals, `I`-prefixed interfaces; one class per file matching the file name; file-scoped namespaces; nullable + implicit usings enabled (`Directory.Build.props`); constructor injection; shared constants in `AlertService.Common/Constants/AlertConstants.cs`; structured Serilog logging with message templates; async methods suffixed `Async` with a trailing `CancellationToken`; guard clauses via `ArgumentNullException.ThrowIfNull`.
- Follow existing patterns; copy the matching example in `project-profile.md` section 6.
- Reuse existing classes, helpers, and validators before creating new ones. No speculative abstractions.
- No hardcoded strings, numbers, paths, or configuration values. No dead code, unused imports, or commented-out code.
- Smallest appropriate visibility. Readable boolean logic; avoid deep nesting.
- Validate nulls, bounds, and failure paths explicitly. Catch specific exceptions; never swallow them.
- Release disposable resources deterministically. Keep concurrency-sensitive code thread-safe.

## Backend / API
- Project conventions (observed): attribute-routed controllers under `/api/alerts` with `[ApiController]`; routes are unversioned (no `v1` segment); `ProducesResponseType` on every action; validation via DTO data-annotation attributes + `IValidatableObject` (model binding returns 400 automatically); enums serialized/accepted as strings (`JsonStringEnumConverter`); services return DTOs mapped by `AlertService.API/Mappings/AlertMappingExtensions.cs`; DI registered in `Program.cs` and `AddSqlDataAccess`; unhandled errors become RFC7807 ProblemDetails via `ExceptionHandlingMiddleware`.
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
- Project conventions (observed): EF Core with SQL Server provider; repository pattern behind `IAlertRepository` (abstraction in `AlertService.Data`, implementation in `AlertService.Data.SQL`); `AsNoTracking()` for read-only queries; filtering/paging/sorting pushed to the database; entity mapping via `IEntityTypeConfiguration` (`ApplyConfigurationsFromAssembly`); `Severity` enum persisted as a string; migrations in `AlertService.Data.SQL/Migrations` managed by the `dotnet-ef` local tool; `EnableRetryOnFailure` configured.
- Select only needed columns (no `SELECT *`); filter in the database; avoid N+1 and whole-table loads.
- Use no-tracking queries for read-only paths when the ORM supports it.
- Keep transactions short and multi-step changes atomic.
- Schema and migration changes need plan approval; never edit objects owned by another team.
- Naming: PascalCase tables and columns; views `v_`; procedures `p_<Entity>_<Verb>`;
  `IX_<Table>_<Columns>`, `PK_<Table>_<Column>`, `FK_<Column>_<RefTable>_<RefColumn>`.

## Integrations
- `NOT_APPLICABLE` — this repository calls no external systems (only its own SQL Server database).

## Frontend / UI
- `NOT_APPLICABLE` — this repository has no frontend.

## Testing
- Project conventions (observed): xUnit `[Fact]` tests in mirror projects (`*.Tests`); Moq for dependencies; `NullLogger<T>` for logging; `Mock<TimeProvider>`/fixed `DateTimeOffset` for deterministic time; repository tests use EF Core InMemory with a fresh `Guid`-named database per test class; method names `Method_Scenario_ExpectedResult`.
- Arrange-Act-Assert; one behavior per test; deterministic; clear names.
- Cover core, edge, and failure scenarios; test behavior, not implementation details.
- Coverage target for new or changed code: as recorded in `project-profile.md` section 7.

## Security
- Validate all input at trust boundaries; parameterized queries and proper output encoding.
- Never hardcode or log secrets, tokens, credentials, or personal data.
- Least privilege for new access-control logic; hiding UI is not authorization.
- Secure defaults (fail closed); never disable certificate validation or security checks.
- Do not leak stack traces, internal paths, or system details in user-facing errors.
