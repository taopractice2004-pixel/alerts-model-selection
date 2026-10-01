# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: Alert management microservice (`AlertService`)
- Primary stack: .NET 8, ASP.NET Core Web API, Entity Framework Core (SQL Server), Serilog, xUnit, Moq
- Repository shape: multi-project .NET solution with layered API/service/data/domain split
- Shared build/runtime settings: `Directory.Build.props` enforces `net8.0`, nullable enabled, implicit usings enabled

## Architecture

- Layering: API controllers -> service layer (`AlertService.API/Services`) -> repository interface (`AlertService.Data`) -> SQL implementation (`AlertService.Data.SQL`) -> domain models (`AlertService.Models`)
- Entry points / composition roots: `AlertService.API/Program.cs` (HTTP pipeline, DI, health, Swagger), `AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs` (data registration/migrations)
- User-facing or external interfaces: REST endpoints under `/api/alerts`, health endpoints `/health/live` and `/health/ready`
- Business logic locations: `AlertService.API/Services/AlertManagementService.cs`
- Data / integration boundaries: `AlertService.Data.Interfaces/IAlertRepository.cs`, EF Core `AlertDbContext`, SQL Server connection `ConnectionStrings:AlertDb`
- Runtime / infrastructure notes: optional startup migrations (`Database:ApplyMigrationsOnStartup`), Serilog console+file logging

## Build And Run

- Install / restore dependencies: `dotnet tool restore` then `dotnet restore`
- Build / compile command: `dotnet build AlertService.sln`
- Test command: `dotnet test`
- Lint / format command: `NOT_CONFIGURED` (no dedicated lint/format command discovered)
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server (`AlertServiceDb`) via EF Core migrations
- Environment/config notes: `AlertService.API/appsettings*.json` contains `ConnectionStrings:AlertDb`, Serilog settings, and migration-on-startup flag
- Operational assets: DB bootstrap/migration scripts in `database/`, health checks, structured logging under `AlertService.API/Logs/`

## Testing

- Main test layers: API controller/service tests and SQL repository tests
- Test frameworks: xUnit, Moq, EF Core InMemory provider for repository tests

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment/runbook documentation: `NOT_AVAILABLE`
- Separate OpenAPI contract file (`openapi*.yaml`): `NOT_AVAILABLE`