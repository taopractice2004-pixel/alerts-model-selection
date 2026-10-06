# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Alert Tagging: introduce a free-form `Tag` concept with a many-to-many relationship to `Alert`,
add endpoints to attach/remove tags, add an optional `tag` filter to the alert list, and surface
tags in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC9). Verification notes: validation rules
  (AC3 dedupe, AC4 max-10, AC5 length) are proven in service/controller tests with boundary
  cases; AC7 404 paths and AC8 filter composition are proven in controller + repository tests.

## Change Strategy
- Follow the existing layered flow end to end: `Tag` entity + `AlertTag` join (Models) →
  `IAlertRepository` methods + tag filter param (Data) → `AlertRepository` EF implementation
  with `Include`/join and a new migration (Data.SQL) → `IAlertService`/`AlertManagementService`
  tag operations (API) → `AlertsController` endpoints → `AlertMappingExtensions` tag mapping →
  `AddTagsRequest` DTO + `AlertResponse.Tags` + `AlertQueryRequest.Tag` (DTO).
- Express tag rules (max 10 per alert, length 1–30) as constants in `AlertConstants`; validate at
  the API boundary via DataAnnotations/`IValidatableObject`, mirroring existing requests.
- Generate the migration with the pinned `dotnet-ef` tool and keep
  `database/02_AlertServiceDb_Migrations.sql` consistent.

## Validation Strategy
- Implementation / bug fix: build only — `dotnet build AlertService.sln` is sufficient to catch
  contract/compile breakage across the touched projects (migration generation is an implementation step).
- Unit testing: controller tests cover endpoint status codes (200/204/400/404) and filter
  wiring; service tests cover dedupe/limit/length rules and not-found handling; repository tests
  cover persistence, `Include` of tags, tag filtering composed with existing filters.

## Boundaries
- Primary slice: alert tag management across Models → Data → Data.SQL → API/DTO.
- Out of scope: tag rename/merge, tag listing/autocomplete endpoints, UI, auth/permission
  changes, and altering existing non-tag alert behavior.

## Requirement Analysis
<!-- Fill only when work_case is AMBIGUOUS or the work is risky/blocked; otherwise write "Not required". -->
- Risks: EF Core many-to-many modeling choice (explicit `AlertTag` join entity recommended to
  match the repo's explicit-configuration convention and to support case-insensitive dedupe);
  migration must stay consistent with the hand-maintained idempotent SQL script; case-sensitivity
  of tag matching must be uniform across add/dedupe, delete, and the list filter.
- Resolution needed from human: confirm the four design defaults recorded in `work.json` →
  `unresolved_questions` (tag normalization/casing, global vs per-alert tag table, POST response
  shape, filter/delete case-insensitivity). Implementation will proceed on the recommended
  defaults unless a reviewer directs otherwise.
