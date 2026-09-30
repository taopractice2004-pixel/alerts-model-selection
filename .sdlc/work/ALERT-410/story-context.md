Story: ALERT-410 — Alert Tagging

Brief: Operators can attach multiple free-form tags to an alert to filter and surface alerts by tag.

Acceptance (summary):
- New `Tag` concept with many-to-many relationship to `Alert` and join table + migration.
- `POST /api/alerts/{id}/tags`: add 1+ tags, dedupe case-insensitively, max 10 tags per alert, length 1–30 chars.
- `DELETE /api/alerts/{id}/tags/{tag}`: remove a tag; return 404 if alert or tag assignment doesn't exist.
- `GET /api/alerts` supports optional `tag` query filter, composable with existing filters.
- Tags included in `AlertResponse`.

Scope anchors (single-line):
- HTTP: `AlertService.API/Controllers/AlertsController.cs` (AddTags, RemoveTag, Get filter)
- DTO: `AlertService.DTO/Responses/AlertResponse.cs` and mapping `AlertService.API/Mappings/AlertMappingExtensions.cs`
- Domain/DB: `AlertService.Models/Alert.cs`, `AlertService.Data.SQL/AlertDbContext.cs`, repositories under `AlertService.Data.SQL/Repositories/`
- DB migration: new EF Core migration in `AlertService.Data.SQL/Migrations/`

Selected standards (referenced):
- standards/api-rest-standards.md
- standards/backend-dotnet-standards.md
- standards/coding-standards.md

Notes: implementation-cache.json is authoritative for exact file lists and commands.
