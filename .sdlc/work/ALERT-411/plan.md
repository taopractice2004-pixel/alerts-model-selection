# Story ALERT-411 - Duplicate Alert Suppression Window

## Story Summary
Suppress near-duplicate alerts on create: when an active alert with the same Title (case-insensitive) and Severity was created within a configurable window (default 15 minutes), return the existing alert with `200 OK` and header `X-Duplicate-Suppressed: true` instead of inserting a new row; genuinely new alerts still return `201 Created`. | Case: SIMPLE

## Acceptance Criteria
- AC1 - On `POST /api/alerts`, if an active alert with the same `Title` (case-insensitive) and the same `Severity` was created within the suppression window, no new row is created.
- AC2 - When suppressed, the endpoint returns `200 OK` with the existing alert body and response header `X-Duplicate-Suppressed: true`.
- AC3 - When no matching active alert exists in the window, a new alert is created and the endpoint returns `201 Created` (existing behavior, no suppression header).
- AC4 - The suppression window length in minutes is read from `appsettings.json` configuration, not hardcoded.
- AC5 - Suppression never applies across different severities, nor when the only matching prior alert is inactive.

## In Scope
- Duplicate detection on create (active + same title case-insensitive + same severity + within window).
- Configurable suppression window via `appsettings.json`.
- Controller returning `200 OK` + header on suppression, `201 Created` otherwise.

## Out of Scope
- Suppression on update (`PUT`) or any endpoint other than `POST /api/alerts`.
- Deduplicating existing rows already in the database.
- Any schema/migration change (none needed).

## Do Not Modify
- `AlertService.Data.SQL/Migrations/**`, `database/**`, `standards/**` (contextPolicy doNotModify). No schema change is required for this story.

## Impacted Files

### Files to Modify
| File | Change | ACs |
|---|---|---|
| [AlertService.Data/Interfaces/IAlertRepository.cs](AlertService.Data/Interfaces/IAlertRepository.cs) | Add `FindActiveDuplicateAsync(title, severity, createdOnOrAfterUtc, ct)` to the contract | AC1, AC5 |
| [AlertService.Data.SQL/Repositories/AlertRepository.cs](AlertService.Data.SQL/Repositories/AlertRepository.cs) | Implement the duplicate query: active + severity match + case-insensitive title + `CreatedDate >= threshold`, newest first | AC1, AC5 |
| [AlertService.API/Services/IAlertService.cs](AlertService.API/Services/IAlertService.cs) | Change `CreateAsync` return type to the new result carrying the alert and a suppressed flag | AC2, AC3 |
| [AlertService.API/Services/AlertManagementService.cs](AlertService.API/Services/AlertManagementService.cs) | In `CreateAsync`, compute the window threshold from configured minutes, look up a duplicate, suppress (return existing + flag) or create | AC1, AC2, AC3, AC4, AC5 |
| [AlertService.API/Controllers/AlertsController.cs](AlertService.API/Controllers/AlertsController.cs) | `Create`: on suppression return `Ok(existing)` + set `X-Duplicate-Suppressed: true`; otherwise `CreatedAtRoute` as today | AC2, AC3 |
| [AlertService.API/Program.cs](AlertService.API/Program.cs) | Bind `AlertSuppressionOptions` from configuration section (wiring) | AC4 |
| [AlertService.API/appsettings.json](AlertService.API/appsettings.json) | Add `AlertSuppression:WindowMinutes` = 15 | AC4 |
| [AlertService.Common/Constants/AlertConstants.cs](AlertService.Common/Constants/AlertConstants.cs) | Add header name, config section name, and default window constants | AC2, AC4 |

### Files to Create
| File | Status | Purpose | ACs |
|---|---|---|---|
| AlertService.API/Configuration/AlertSuppressionOptions.cs | PROPOSED | Options class with `WindowMinutes` bound from `appsettings.json` | AC4 |
| AlertService.API/Services/CreateAlertResult.cs | PROPOSED | Small result type (`AlertResponse Alert`, `bool WasSuppressed`) returned by `CreateAsync` | AC2, AC3 |

## Reuse / Existing Patterns
| Change | Example file to copy | Reuse instead of creating |
|---|---|---|
| Repository query | [AlertService.Data.SQL/Repositories/AlertRepository.cs](AlertService.Data.SQL/Repositories/AlertRepository.cs) (`GetAllAsync` title `ToLower()` filter) | Reuse `AlertDbContext.Alerts`; same case-insensitive `ToLower()` comparison style |
| Service logic | [AlertService.API/Services/AlertManagementService.cs](AlertService.API/Services/AlertManagementService.cs) (`CreateAsync`, `_timeProvider`) | Reuse `_timeProvider`, `_repository`, `ToEntity`/`ToResponse` mappings and structured logging |
| Config constant | [AlertService.Common/Constants/AlertConstants.cs](AlertService.Common/Constants/AlertConstants.cs) | Add constants here; do not inline literals |
| DI / config binding | [AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs](AlertService.Data.SQL/Extensions/ServiceCollectionExtensions.cs), [AlertService.API/Program.cs](AlertService.API/Program.cs) | Reuse existing composition root; bind options there |
| Controller status/header | [AlertService.API/Controllers/AlertsController.cs](AlertService.API/Controllers/AlertsController.cs) (`Create`) | Keep thin; add header via `Response.Headers` |

## Implementation Plan

### Contracts
- `POST /api/alerts`: request unchanged (`CreateAlertRequest`). Responses: `201 Created` (`AlertResponse`, existing `CreatedAtRoute` to `GetById`) for a new alert; `200 OK` (`AlertResponse` of the existing alert) with response header `X-Duplicate-Suppressed: true` when suppressed; `400 Bad Request` unchanged for invalid input.
- `IAlertService.CreateAsync(CreateAlertRequest request, CancellationToken) -> Task<CreateAlertResult>` where `CreateAlertResult` has `AlertResponse Alert` and `bool WasSuppressed`.
- `IAlertRepository.FindActiveDuplicateAsync(string title, Severity severity, DateTime createdOnOrAfterUtc, CancellationToken) -> Task<Alert?>` (returns the most recent matching active alert, or `null`).
- `AlertSuppressionOptions { int WindowMinutes }`, config section `AlertSuppression`, key `AlertSuppression:WindowMinutes` (default `15`).
- Constants added to `AlertConstants`: `DuplicateSuppressedHeader = "X-Duplicate-Suppressed"`, `DuplicateSuppressedHeaderValue = "true"`, `AlertSuppressionSection = "AlertSuppression"`, `DefaultSuppressionWindowMinutes = 15`.

### Behavior Rules
- Duplicate match = `IsActive == true` AND `Severity` equal AND `Title` equal ignoring case AND `CreatedDate >= now - WindowMinutes`; title compared on the trimmed value to match create-time trimming (AC1).
- Only active alerts qualify; an inactive prior alert is never a duplicate (query filters `IsActive`) (AC5).
- Different severity never matches (query filters `Severity`) (AC5).
- When multiple active duplicates exist, return the most recent by `CreatedDate` (AC2).
- Window length comes from `AlertSuppressionOptions.WindowMinutes`; the service never hardcodes 15; `now` comes from the injected `TimeProvider` (AC4).
- On suppression: do not call `AddAsync`; log at Information that the alert was suppressed (include existing alert id); return existing alert + `WasSuppressed = true` (AC1, AC2).
- On no match: create as today and return `WasSuppressed = false` (AC3).
- Controller sets the suppression header only when `WasSuppressed` is true and returns `200 OK`; otherwise `201 Created` (AC2, AC3).

### Steps
1. [AC1, AC5] [AlertService.Data/Interfaces/IAlertRepository.cs](AlertService.Data/Interfaces/IAlertRepository.cs) -> `IAlertRepository`: add `FindActiveDuplicateAsync` signature.
2. [AC1, AC5] [AlertService.Data.SQL/Repositories/AlertRepository.cs](AlertService.Data.SQL/Repositories/AlertRepository.cs) -> `AlertRepository.FindActiveDuplicateAsync`: `AsNoTracking` query filtering active + severity + `Title.ToLower() == title.Trim().ToLower()` + `CreatedDate >= createdOnOrAfterUtc`, order by `CreatedDate` desc, `FirstOrDefaultAsync`; follow `GetAllAsync` filter style.
3. [AC2, AC3] AlertService.API/Services/CreateAlertResult.cs -> `CreateAlertResult`: record with `AlertResponse Alert`, `bool WasSuppressed`.
4. [AC4] AlertService.API/Configuration/AlertSuppressionOptions.cs -> `AlertSuppressionOptions`: `int WindowMinutes` defaulting to `AlertConstants.DefaultSuppressionWindowMinutes`.
5. [AC2, AC4] [AlertService.Common/Constants/AlertConstants.cs](AlertService.Common/Constants/AlertConstants.cs) -> add header name, header value, section name, and default window constants.
6. [AC1, AC2, AC3, AC4, AC5] [AlertService.API/Services/AlertManagementService.cs](AlertService.API/Services/AlertManagementService.cs) -> inject `IOptions<AlertSuppressionOptions>`; in `CreateAsync` compute `threshold = now - TimeSpan.FromMinutes(WindowMinutes)`, call `FindActiveDuplicateAsync`; if found, log + return existing mapped with `WasSuppressed = true`; else create and return `WasSuppressed = false`; update return type to `CreateAlertResult`.
7. [AC2, AC3] [AlertService.API/Services/IAlertService.cs](AlertService.API/Services/IAlertService.cs) -> change `CreateAsync` return type to `Task<CreateAlertResult>`.
8. [AC2, AC3] [AlertService.API/Controllers/AlertsController.cs](AlertService.API/Controllers/AlertsController.cs) -> `Create`: on `WasSuppressed` set `Response.Headers[AlertConstants.DuplicateSuppressedHeader]` and return `Ok(result.Alert)`; else `CreatedAtRoute` as today; add `ProducesResponseType(200)`.
9. [wiring][AC4] [AlertService.API/Program.cs](AlertService.API/Program.cs) -> `builder.Services.Configure<AlertSuppressionOptions>(builder.Configuration.GetSection(AlertConstants.AlertSuppressionSection))`.
10. [wiring][AC4] [AlertService.API/appsettings.json](AlertService.API/appsettings.json) -> add `"AlertSuppression": { "WindowMinutes": 15 }`.

## Validation Plan
- Build / type check: `AlertService.sln` (changes span API, Data, Data.SQL, Common; type check is part of the C# build).
- Lint / static analysis: NOT_CONFIGURED.
- Regression: existing `AlertManagementServiceTests`, `AlertsControllerTests`, `AlertRepositoryTests`.
- Integration / functional: NOT_CONFIGURED (no separate integration suite; manual check of `POST /api/alerts` twice within the window is optional).
- Coverage: NOT_CONFIGURED (optional `--collect:"XPlat Code Coverage"` on the two test projects).

## Unit Test Plan
| Test file | New or existing | Behaviors and edge cases |
|---|---|---|
| [AlertService.API.Tests/Services/AlertManagementServiceTests.cs](AlertService.API.Tests/Services/AlertManagementServiceTests.cs) | EXISTING | CreateAsync: suppresses when an active duplicate is returned (no `AddAsync`, `WasSuppressed` true, returns existing); creates when none (`AddAsync` called, `WasSuppressed` false); passes threshold computed from configured `WindowMinutes` and `TimeProvider`; passes trimmed title and request severity to `FindActiveDuplicateAsync` |
| [AlertService.API.Tests/Controllers/AlertsControllerTests.cs](AlertService.API.Tests/Controllers/AlertsControllerTests.cs) | EXISTING | Create returns `201 CreatedAtRoute` when not suppressed and no header; returns `200 Ok` with existing alert and `X-Duplicate-Suppressed: true` header when suppressed (set `ControllerContext` with `DefaultHttpContext`) |
| [AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs](AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs) | EXISTING | FindActiveDuplicateAsync: matches active same-title (case-insensitive) same-severity within window; ignores inactive; ignores different severity; ignores alerts created before the threshold; returns most recent when multiple match; returns null when none |

## AC-to-Validation Mapping
| AC | Validation type | Validation (test name and file, or check) | Expected result |
|---|---|---|---|
| AC1 | UNIT_TEST | `FindActiveDuplicateAsync_*` in AlertRepositoryTests; `CreateAsync_WhenActiveDuplicateWithinWindow_Suppresses` in AlertManagementServiceTests | No new row; existing alert returned; `AddAsync` not called |
| AC2 | UNIT_TEST | `Create_WhenSuppressed_Returns200WithHeader` in AlertsControllerTests | `OkObjectResult` with existing `AlertResponse`; header `X-Duplicate-Suppressed: true` |
| AC3 | UNIT_TEST | `Create_WhenNew_Returns201Created` in AlertsControllerTests; `CreateAsync_WhenNoDuplicate_Creates` in AlertManagementServiceTests | `CreatedAtRouteResult` 201; `AddAsync` called; no suppression header |
| AC4 | UNIT_TEST | `CreateAsync_UsesConfiguredWindowMinutes` in AlertManagementServiceTests | Threshold passed = `now - configured WindowMinutes`; value sourced from options, not a literal |
| AC5 | UNIT_TEST | `FindActiveDuplicateAsync_IgnoresInactive`, `FindActiveDuplicateAsync_IgnoresDifferentSeverity` in AlertRepositoryTests | No match returned for inactive or different-severity priors |

## Dependencies / Risks
- Dependencies: `Microsoft.Extensions.Options` (part of ASP.NET Core; no new package).
- Risks: `CreateAsync` return type change is a non-additive contract change on the internal `IAlertService`; callers (controller, tests) updated in the same story. Low external risk (interface is internal to the API).
- Stop points: None (no Shared Files; no schema change).

## Open Questions
- Blocking: None.
- Non-blocking: Confirm config section/key name `AlertSuppression:WindowMinutes` and that a non-positive value should simply disable suppression (plan assumes the configured value is used as-is).
- Open decisions (greenfield architecture or technical; blocking until decided): None.

## Approval Status
Status: APPROVED
Approved by: developer
Date: 2026-10-02
