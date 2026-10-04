# Plan — ALERT-410

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add free-form tagging to alerts: a new `Tag` entity with a many-to-many relationship to `Alert`
(new join table + migration), add/remove tag endpoints, an optional `tag` filter on the alert
list, and tags surfaced in `AlertResponse`.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria` (AC1–AC9). Verification notes:
  - AC3/AC4/AC7/AC8 are the risk-bearing behaviors (case-insensitive dedupe/match, the max-10 and
    1–30 limits, and tag-filter composability) and must each get explicit `/unit-testing` cases.

## Change Strategy
- Introduce `Tag` as a shared entity (unique tag name) with a many-to-many skip navigation to
  `Alert`, configured via a new `TagConfiguration` using the existing
  `ApplyConfigurationsFromAssembly` convention; add an EF Core migration plus a matching
  `database/*.sql` join-table script.
- Extend the existing layered flow rather than adding a new one: new controller actions →
  `IAlertService` tag methods → `IAlertRepository` tag persistence, keeping EF out of the service
  and DTOs out of the repository.
- Add the `tag` filter as one more optional predicate inside `AlertRepository.GetAllAsync`,
  mirroring the existing case-insensitive `search` predicate so it composes with the current
  filters, and thread a new `AlertQueryRequest.Tag` through the service call.
- Centralize the max-10 and 1–30 limits in `AlertConstants`; dedupe/normalize tags case-
  insensitively; map tags into `AlertResponse` in `AlertMappingExtensions`.

## Validation Strategy
- Implementation / bug fix: build only — `dotnet build AlertService.sln` is sufficient because the
  change is standard typed .NET across existing projects and the compiler catches signature/DI
  wiring breaks (`work.json` → `build_commands`).
- Unit testing: controller + service tests cover the add/remove/filter contracts and 404 paths;
  repository (Sqlite) tests prove the join-table persistence, case-insensitive dedupe/match, and
  that the `tag` filter composes with existing filters; a mapping/response assertion proves tags
  appear in `AlertResponse`.

## Boundaries
- Primary slice: alert tagging across controller → service → repository plus the `Tag` entity,
  EF configuration/migration, and DTO changes.
- Out of scope: alert sorting, paging, summary aggregation, severity logic, and any
  non-tag alert fields; no new tagging analytics or tag-management (list/rename) endpoints beyond
  the three described operations.

## Requirement Analysis
<!-- AMBIGUOUS: design decisions below need confirmation; proceeding on the stated assumptions. -->
- Risks:
  - `AlertResponse` tag shape (plain strings vs objects) — assumed a list of plain strings to match
    free-form tags; changing later would alter the response contract.
  - DELETE `{tag}` route handling of special characters (e.g. `.`, `/`, spaces) requires URL
    encoding and possibly a catch-all/route constraint; assumed an encoded path segment matched
    case-insensitively.
  - Tag identity: assumed shared, unique (case-insensitive) tag names referenced by the join
    table, so the same tag reused across alerts is one row; an alternative per-alert duplication
    would change the schema.
- Resolution needed from human: confirm the three assumptions above (recorded in `work.json` →
  `unresolved_questions`) before or during `/implement-story`; defaults are safe to proceed on.
