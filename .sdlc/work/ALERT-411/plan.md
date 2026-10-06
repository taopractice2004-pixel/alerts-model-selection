# Plan - ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` - reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add near-duplicate suppression for alert creation so POST /api/alerts returns an existing active alert (200 + suppression header) when title/severity duplicates are found inside a configurable suppression window.

## Acceptance Criteria / Bug Behavior / Test Target
- Acceptance criteria are owned by `work.json` -> `acceptance_criteria`.
- `/unit-testing` should verify both response semantics (200 with suppression header vs 201 create) and duplicate matching rules (active-only, same severity, case-insensitive title, configured window).

## Change Strategy
- Extend the existing create flow with a pre-create duplicate check in service/repository, preserving current create behavior when no duplicate is found.
- Keep API contract changes minimal by returning existing alert data with explicit suppression signaling in response headers.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) for compile validation across API/service/data layers.
- Unit testing: run targeted API and SQL repository tests listed in `work.json` to validate AC1-AC5 behavior.

## Boundaries
- Primary slice: alert create endpoint/service/repository path and API configuration for suppression window.
- Out of scope: changes to non-create endpoints, unrelated alert lifecycle behavior, and UI/client modifications.

## Requirement Analysis
- Risks: duplicate selection behavior is not explicitly defined when multiple active matches are present inside the suppression window.
- Resolution needed from human: confirm deterministic tie-break preference (newest vs oldest matching active alert) if business behavior requires a specific one.
