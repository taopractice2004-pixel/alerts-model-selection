# ALERT-410 Impact Map

## In Scope
- Alert API route surface for tag assignment/removal and list filtering.
- Service/repository behavior for tag validation, persistence, and query composition.
- EF Core schema/model updates for many-to-many Alert-Tag support.
- Alert response shape including tags.

## Adjacent Dependencies
- Existing alert list filtering/sorting/paging behavior must remain unchanged when tag is absent.
- Existing duplicate-suppression and summary behavior are out of scope unless regressed by shared query/model changes.
- Existing trends and duplicate-suppression endpoints are currently absent in API source and cause unrelated failures in full `dotnet test` integration coverage (ALERT-411/ALERT-412 scenarios).

## Out of Scope
- New standalone tag management endpoints.
- Frontend/UI changes.
- Non-alert domain entities.
- Implementing ALERT-411 duplicate suppression and ALERT-412 trends behavior.
