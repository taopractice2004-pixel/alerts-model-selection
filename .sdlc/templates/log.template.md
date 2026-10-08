# Log — {{ID}}

> Workflow state plus one entry per stage run. Update the status header in place and append a
> new entry at the bottom — never rewrite earlier entries. Work type, case, effort mode, files,
> commands, acceptance criteria, open bugs, and review findings are owned by `work.json`; record
> only what actually happened here.

## Current Stage
{{STORY_ANALYSIS | PLAN_REVIEW | IMPLEMENTATION | UNIT_TESTING | BUG_FIX | CODE_REVIEW | ESCALATED_TO_DEVELOPER | COMPLETE}}

| Stage | Status |
|---|---|
| Story Analysis | NOT_STARTED |
| Plan Review | {{PENDING | APPROVED | NOT_REQUIRED}} |
| Implementation | NOT_STARTED |
| Unit Testing | NOT_STARTED |
| Bug Fix | NOT_STARTED |
| Code Review | NOT_STARTED |
| Test → Fix Loop | {{fix_iteration}}/3 — {{NOT_STARTED | TESTS_PASSED | BUGS_OPEN | RETEST_REQUIRED | ESCALATED_TO_DEVELOPER}} |
| Review → Fix Loop | {{review_fix_iteration}}/2 — {{NOT_STARTED | PASSED | FINDINGS_OPEN | FIXES_APPLIED | ESCALATED_TO_DEVELOPER}} |

## Entries

### {{timestamp}} — {{/command}} — {{STAGE_PASSED | STAGE_FAILED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
- Summary: {{what this run did}}
- Files changed: {{paths or None (analysis / review only)}}
- Build: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Unit tests: {{command → passed/failed counts, or NOT_RUN (not run in this stage)}}
- Acceptance criteria: {{AC id → MET | NOT_MET | NOT_VERIFIABLE, or NOT_RUN}}
- Coverage: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Bugs: {{bug ids opened / fixed, or None}}
- Review: {{analyzer → result; findings opened / fixed / resolved / waived by id and rule, or NOT_RUN}}
- Loop: {{fix_iteration}}/3 · review {{review_fix_iteration}}/2
- Next recommended command: {{command or None — reason}}
