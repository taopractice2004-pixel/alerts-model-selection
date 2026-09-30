Implementation Plan (ALERT-410) — Minimal, reviewable steps

1) Add domain model and DB mapping
- Create `Tag` entity and many-to-many join (AlertTag). Add EF Core migration.
- Update `AlertDbContext` to include DbSet<Tag> and join configuration.

2) Persistence + repository
- Extend `AlertRepository` (or add repository methods) to add/remove tags, and query by tag.

3) API endpoints
- Implement `POST /api/alerts/{id}/tags` to add tags (validation: 1–30 chars, case-insensitive dedupe, max 10 tags per alert).
- Implement `DELETE /api/alerts/{id}/tags/{tag}` to remove; return 404 when appropriate.
- Update `GET /api/alerts` to accept optional `tag` filter and apply within existing query pipeline.

4) DTOs & mapping
- Include `tags` collection (string[]) in `AlertResponse` and map from domain entity.

5) Tests
- Add/extend unit and integration tests: controller tests, repository tests, and service tests covering validation, dedupe, max-tags, add/remove, and GET filtering.

6) Migration and database
- Add EF migration and update DB scripts; ensure tests use in-memory or test DB changes.

7) Docs & standards
- Update API contract (OpenAPI/HTTP file) and note constraints (max 10 tags, length). Follow `api-rest-standards.md`.

Validation commands (development): `dotnet build AlertService.sln` and `dotnet test` for affected test projects.
