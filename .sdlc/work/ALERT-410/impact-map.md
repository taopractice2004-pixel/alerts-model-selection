Impact Map — ALERT-410 (high-level)

- `AlertService.API/Controllers/AlertsController.cs`: add endpoints, routing, and HTTP semantics.
- `AlertService.API/Services/AlertManagementService.cs` and `IAlertService.cs`: business logic for add/remove tags and GET filtering.
- `AlertService.API/Mappings/AlertMappingExtensions.cs` and `AlertService.DTO/Responses/AlertResponse.cs`: include tags in response.
- `AlertService.Models/Alert.cs`: navigation collection for tags.
- `AlertService.Data.SQL/AlertDbContext.cs` and `AlertService.Data.SQL/Repositories/AlertRepository.cs`: EF mappings, migration, repository methods.
- Tests: `AlertService.API.Tests/Controllers/AlertsControllerTests.cs`, `AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs`, service tests.

Backward compatibility:
- GET /api/alerts without `tag` should remain unchanged.
- Adding tags must not change existing alert payload fields except adding `tags` array (nullable/empty by default).

Operational notes:
- Migration adds a join table; coordinate DB deployment with migration.
- Enforce maximum tags per alert transactionally.
# Impact Map - ALERT-410

**Direct path:** REST routes and query DTO -> alert service -> repository contract/SQL implementation -> EF model/configuration/migration -> alert response mapping.

**Behavioral dependencies:** Existing pagination, sorting, search, date, severity, and active filters must remain composable with `tag`. Alert reads must load tags consistently before mapping. Alert deletion must not leave orphaned assignments.

**Operational edge:** Database migration and the corresponding idempotent database script must be reviewed for existing environments. OpenAPI is generated at runtime; no committed contract was found.

**Data decision:** Tag rows are globally shared by case-insensitive name, and the explicit `AlertTags` join table cascades assignments when an alert is deleted.

**Out of scope:** UI/client changes, summary filtering, deployment/CI changes, and unrelated story expectations.
