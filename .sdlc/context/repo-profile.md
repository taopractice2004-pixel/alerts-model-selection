# Repository Profile

- Repository mode: `MODE_C_EXISTING_PROJECT`
- Product: AlertService alert management microservice
- Primary stack: C#, ASP.NET Core Web API on .NET 8, EF Core 8, SQL Server, Serilog, Swagger, xUnit, Moq
- Repository shape: Multi-project .NET solution with API, domain/contracts, data abstraction, SQL implementation, tests, SQL scripts, and standards docs
- Shared build/runtime settings: `Directory.Build.props` sets `net8.0`, nullable enabled, implicit usings enabled; local tool manifest includes `dotnet-ef` 8.0.31

## Architecture

- Layering: Controllers handle HTTP, services own business logic, `AlertService.Data` exposes provider-agnostic repository contracts, `AlertService.Data.SQL` owns EF Core and SQL Server persistence
- Entry points / composition roots: `AlertService.API/Program.cs` configures Serilog, MVC, Swagger, health checks, SQL data access, DI, middleware, and optional migration-on-startup behavior
- User-facing or external interfaces: REST endpoints under `/api/alerts`, health endpoints under `/health/live` and `/health/ready`, runtime Swagger UI in Development
- Business logic locations: `AlertService.API/Services`, request/response mapping in `AlertService.API/Mappings`, shared constants/enums in `AlertService.Common`
- Data / integration boundaries: `AlertService.Data/Interfaces/IAlertRepository.cs`, `AlertService.Data.SQL/Repositories`, `AlertService.Data.SQL/AlertDbContext.cs`, SQL scripts under `database/`
- Runtime / infrastructure notes: Global exception middleware returns problem details, health checks include DB readiness, optional EF migrations run at startup when `Database:ApplyMigrationsOnStartup` is true

## Build And Run

- Install / restore dependencies: `dotnet tool restore` and `dotnet restore`
- Build / compile command: `dotnet build AlertService.sln`
- Test command: `dotnet test`
- Lint / format command: `NOT_CONFIGURED`
- Run main app locally: `dotnet run --project AlertService.API --launch-profile https`

## Data And Operations

- Primary data store: SQL Server via EF Core (`AlertDbContext`)
- Environment/config notes: Default connection string is `ConnectionStrings:AlertDb` in `AlertService.API/appsettings*.json`; README documents LocalDB and Docker SQL Server options; newer SDKs may require `DOTNET_ROLL_FORWARD=Major`
- Operational assets: EF Core migrations in `AlertService.Data.SQL/Migrations`, idempotent SQL scripts in `database/`, HTTP scratch file in `AlertService.API/AlertService.API.http`, Serilog file output in `AlertService.API/Logs/`

## Testing

- Main test layers: API controller/service tests plus health/integration-style tests in `AlertService.API.Tests`; repository/data-access tests in `AlertService.Data.SQL.Tests`
- Test frameworks: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory, EF Core Sqlite, Coverlet collector

## Known Gaps / Discover Later

- CI/CD pipeline definition: `NOT_AVAILABLE`
- Deployment/runbook documentation: `NOT_AVAILABLE`
- Separate checked-in OpenAPI contract artifact: `NOT_AVAILABLE`
