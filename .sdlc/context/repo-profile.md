# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: AlertService alert-management microservice
- Stack: .NET 8 (`net8.0`), ASP.NET Core Web API, C#, EF Core 8, SQL Server, Serilog, Swagger, xUnit, Moq
- Solution shape: Multi-project layered solution with API host, shared/domain libraries, data abstraction + SQL implementation, and test projects

## Architecture Snapshot

- Composition root: `AlertService.API/Program.cs`
- Layering: `Controllers -> Services -> AlertService.Data (interfaces) -> AlertService.Data.SQL (EF Core)`
- Shared/domain projects: `AlertService.Common`, `AlertService.Models`, `AlertService.DTO`
- Persistence: `AlertService.Data.SQL/AlertDbContext.cs`, repository implementations under `AlertService.Data.SQL/Repositories`
- API surface: `/api/alerts` CRUD + list + summary, health endpoints `/health/live` and `/health/ready`

## Build/Test/Run

- Restore tools/packages: `dotnet tool restore`, `dotnet restore`
- Build: `dotnet build AlertService.sln`
- Test: `dotnet test`
- Run API: `dotnet run --project AlertService.API --launch-profile https`
- Lint/format command: `NOT_CONFIGURED`

## Data/Operations

- Data store: SQL Server (`AlertServiceDb`) via connection string `ConnectionStrings:AlertDb`
- Migrations: `AlertService.Data.SQL/Migrations`
- SQL scripts: `database/01_CreateDatabase.sql`, `database/02_AlertServiceDb_Migrations.sql`

## Known Gaps

- CI/CD pipeline config: `NOT_AVAILABLE`
- Deployment runbook: `NOT_AVAILABLE`
- Committed OpenAPI contract file (YAML/JSON): `NOT_AVAILABLE`
