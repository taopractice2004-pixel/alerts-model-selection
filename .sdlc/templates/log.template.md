# Log — {{ID}}

> Workflow state plus one entry per stage run. Update the status header in place and append a
> new entry at the bottom — never rewrite earlier entries. Work type, case, effort mode, files,
> commands, acceptance criteria, and open bugs are owned by `work.json`; record only what
> actually happened here.

## Current Stage
{{STORY_ANALYSIS | IMPLEMENTATION | UNIT_TESTING | BUG_FIX | ESCALATED_TO_DEVELOPER | PREPARE_PR | L0_REVIEW | L1_REVIEW | ADDRESS_REVIEW_<l0|l1> | PR_REVIEW | COMPLETE}}

| Stage | Status |
|---|---|
| Story Analysis | NOT_STARTED |
| Implementation | NOT_STARTED |
| Unit Testing | NOT_STARTED |
| Bug Fix | NOT_STARTED |
| Test → Fix Loop | {{fix_iteration}}/3 — {{NOT_STARTED | TESTS_PASSED | BUGS_OPEN | ESCALATED_TO_DEVELOPER}} |
| Prepare PR | NOT_STARTED |
| L0 Review | NOT_STARTED |
| L1 Review | NOT_STARTED |

## Entries

### {{timestamp}} — {{/command}} — {{STAGE_PASSED | STAGE_FAILED | CHANGES_REQUIRED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
- Summary: {{what this run did}}
- Files changed: {{paths or None (analysis/review only)}}
- Build: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Unit tests: {{command → passed/failed counts, or NOT_RUN (not run in this stage)}}
- Acceptance criteria: {{AC id → MET | NOT_MET | NOT_VERIFIABLE, or NOT_RUN}}
- Coverage: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Bugs: {{bug ids opened / fixed, or None}}
- Review: {{L0/L1 findings opened / resolved, or None}}
- Loop: {{fix_iteration}}/3
- Standards notes: {{deviation from selected standards, or None}}
- Deferred: {{item or None}}
- Next recommended command: {{command or None — reason}}
