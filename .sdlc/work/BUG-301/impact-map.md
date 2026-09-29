# BUG-301 Impact Map

## In Scope
- AlertService.Data.SQL/Repositories/AlertRepository.cs: severity sorting translation
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs: regression coverage for relational severity ordering
- AlertService.Data.SQL.Tests/AlertService.Data.SQL.Tests.csproj: relational test provider dependency if required

## Out Of Scope
- API/controller/service contract changes
- DTO or domain model changes
- Database schema, migrations, and SQL scripts

## Validation Surface
- Focused repository severity-sort test
- Repository test project
- Solution build
