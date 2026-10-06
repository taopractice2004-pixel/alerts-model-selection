# Plan — {{ID}}

> Thin human-review summary. Exact files, scope anchors, adjacent dependencies, selected
> standards, commands, constraints, missing facts, and unresolved questions are owned by
> `work.json` — reference it, do not repeat it. Overwrite this file fully when re-planning.

## Summary
{{short story summary, bug behavior, or testcase target}}

## Acceptance Criteria / Bug Behavior / Test Target
- Owned by `work.json` → `acceptance_criteria`; note here only how they will be verified in
  `/unit-testing` when that is not obvious.

## Change Strategy
- {{smallest change approach for the scoped slice}}
- {{root-cause note or implementation constraint}}

## Validation Strategy
- Implementation / bug fix: build only ({{why the build command in work.json is sufficient}})
- Unit testing: {{how the unit tests prove each acceptance criterion}}

## Boundaries
- Primary slice: {{main source area or entry point}}
- Out of scope: {{boundary intentionally not touched or None}}

## Requirement Analysis
<!-- Fill only when work_case is AMBIGUOUS or the work is risky/blocked; otherwise write "Not required". -->
- Risks: {{item or None}}
- Resolution needed from human: {{item or None}}
