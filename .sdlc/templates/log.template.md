# Log — {{ID}}

> Workflow state plus one entry per stage run. Update the status header in place and append a
> new entry at the bottom — never rewrite earlier entries. Work type, case, effort mode, files,
> commands, acceptance criteria, and open bugs are owned by `work.json`; record only what
> actually happened here.

## Current Stage
{{STORY_ANALYSIS | IMPLEMENTATION | UNIT_TESTING | BUG_FIX | ESCALATED_TO_DEVELOPER | COMPLETE}}

| Stage | Status |
|---|---|
| Story Analysis | NOT_STARTED |
| Implementation | NOT_STARTED |
| Unit Testing | NOT_STARTED |
| Bug Fix | NOT_STARTED |
| Test → Fix Loop | {{fix_iteration}}/3 — {{NOT_STARTED | TESTS_PASSED | BUGS_OPEN | ESCALATED_TO_DEVELOPER}} |

## Entries

### {{timestamp}} — {{/command}} — {{STAGE_PASSED | STAGE_FAILED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
- Summary: {{what this run did}}
- Files changed: {{paths or None (analysis only)}}
- Build: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Unit tests: {{command → passed/failed counts, or NOT_RUN (not run in this stage)}}
- Acceptance criteria: {{AC id → MET | NOT_MET | NOT_VERIFIABLE, or NOT_RUN}}
- Coverage: {{command → result, NOT_CONFIGURED, or NOT_RUN}}
- Bugs: {{bug ids opened / fixed, or None}}
- Loop: {{fix_iteration}}/3
- Standards notes: {{deviation from selected standards, or None}}
- Deferred: {{item or None}}
- Next recommended command: {{command or None — reason}}
