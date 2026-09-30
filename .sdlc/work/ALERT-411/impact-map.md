# Impact Map - ALERT-411

## Primary Slice
- Duplicate suppression within the existing `POST /api/alerts` controller -> service -> repository flow, plus API configuration wiring for the suppression window.

## Adjacent Dependencies
- AlertService.API/Program.cs
- AlertService.API/appsettings.json
- AlertService.API/Services/IAlertService.cs
- AlertService.Data/Interfaces/IAlertRepository.cs

## Out Of Scope
- Database schema or migration changes.
- Duplicate suppression on non-create endpoints.
- Cross-severity suppression rules beyond the explicit story criteria.
- UI or client-side changes.

## Cache Reference
- Exact touched source files and tests are owned by `implementation-cache.json`.