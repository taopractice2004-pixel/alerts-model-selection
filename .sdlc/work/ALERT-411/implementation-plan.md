Implementation Plan (compact)

1. Add a configurable suppression window setting to `appsettings.json` (suggest section `AlertDeduplication:SuppressionWindowMinutes`, default 15).
2. Add an options POCO (e.g., `AlertDeduplicationOptions`) and bind it in `Program.cs`.
3. Implement duplicate detection in `AlertManagementService.CreateAsync`:
   - Before calling `_repository.AddAsync`, query repository for an active alert with same Title (case-insensitive) and same Severity created on or after `UtcNow - suppressionWindow`.
   - If found, return the existing alert DTO and surface `X-Duplicate-Suppressed: true` at the controller level.
   - Otherwise, proceed to create as today.
4. Add repository helper if needed: extend `IAlertRepository` with a single method `FindActiveByTitleAndSeveritySinceAsync(string title, Severity severity, DateTime since)` and implement in `AlertRepository` (EF query using normalized Title equality and IsActive filter).
5. Update `AlertsController.Create` to inspect the service result and return either `201 Created` (new) or `200 OK` with header `X-Duplicate-Suppressed: true` (suppressed). Alternatively have service return a flag or a wrapper result to indicate suppression.
6. Update unit tests:
   - `AlertManagementServiceTests`: add tests for suppression when repository returns matching active alert within window and ensure no `AddAsync` is called; update constructor usage to supply options/IOptions or mock resolver.
   - `AlertsControllerTests`: add test expecting `200 OK` and header when suppression occurs, and ensure existing tests that expect `201 Created` still pass for new alerts.
7. Run `dotnet test` and iterate until green. Keep changes minimal and update `changes.md`.

Risks & mitigations
- Tests: constructor signature change for `AlertManagementService` will require updating test setup. Mitigate by providing an overload or optional parameter during transition, or update tests accordingly.
- Placement: implementing in service keeps DI and business logic centralized; implementing in repository couples persistence to business rule. Recommend service-layer implementation with a small repository query helper.
