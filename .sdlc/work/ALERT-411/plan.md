# Plan - ALERT-411

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` - reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
Add near-duplicate suppression for alert creation so repeated active alerts with the same title and severity inside a configurable time window return the existing alert instead of creating a new record.

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` -> `acceptance_criteria`.
- Verification in `/unit-testing` should prove both response shape variants for POST: suppressed duplicate (`200` + header) and true create (`201`), plus window/severity/active-state boundaries.

## Change Strategy
- Keep the existing POST flow and add a targeted pre-create duplicate check in the service/data path, then branch controller response metadata based on service result.
- Read suppression window minutes from configuration and keep default behavior deterministic and explicit.

## Validation Strategy
- Implementation / bug fix: build only (`dotnet build AlertService.sln`) to ensure compile-time integrity across controller, service, and repository changes.
- Unit testing: targeted controller/service/repository tests (defined in `work.json`) validate suppression matching, excluded cases, response codes, and response header behavior.

## Boundaries
- Primary slice: alert POST endpoint and its service/repository path, plus alert API configuration and service option wiring for suppression window.
- Out of scope: unrelated alert endpoints, query/listing behavior, and non-alert domain logic.

## Requirement Analysis
Not required.
