# Log - {{ID}}

<!-- log.md starts with only the line "# Log - <ID>". Each stage APPENDS one entry in the format of
     its own block below. Never rewrite earlier entries. -->

## {{timestamp}} - /analyze-story - {{STAGE_PASSED | WAITING_FOR_HUMAN}}
- Re-analysis: {{No | Yes - planned files changed since the previous analysis: <files or None>}}
- Summary: {{number of ACs, files to modify/create, blocking questions}}
- Status: ANALYSIS_DRAFT

## {{timestamp}} - /implement-story - {{STAGE_PASSED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
- Staleness check: {{plan current | changed since analysis: <files> - developer chose continue | SKIPPED (reason)}}
- Implementation Plan steps done: {{1, 2, 3, or steps not done and why}}
- Files modified: {{paths}}
- Files created: {{paths or None}}
- Minor deviations: {{change and reason, or None}}
- Scope check: {{only planned or recorded files changed; no debug or commented-out code; nothing duplicated}}
- Status: {{IMPLEMENTATION_COMPLETE | IMPLEMENTATION_IN_PROGRESS}}

## {{timestamp}} - /unit-testing - {{STAGE_PASSED | STAGE_FAILED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
- Tests created or updated: {{paths and test names}}

| Check | Command | Result |
|---|---|---|
| Build | {{command}} | {{PASS / FAIL / NOT_RUN (reason)}} |
| Type check | {{command}} | {{PASS / FAIL / NOT_CONFIGURED}} |
| Unit tests | {{command}} | {{PASS (n passed) / FAIL (n failed) / NOT_RUN}} |
| Regression tests | {{command}} | {{PASS / FAIL / NOT_RUN}} |
| Integration tests | {{command}} | {{PASS / FAIL / NOT_CONFIGURED / NOT_RUN}} |
| Lint | {{command}} | {{PASS / FAIL / NOT_CONFIGURED}} |
| Coverage | {{command}} | {{n% vs target / NOT_CONFIGURED}} |

| AC | Validation type | Validation | Result |
|---|---|---|---|
| AC1 | {{UNIT_TEST / UI_COMPONENT_TEST / BUILD_CHECK / INTEGRATION_FUNCTIONAL}} | {{test or check}} | {{PASS / FAIL / NOT_VERIFIED (reason)}} |

- Failure routing: {{failure -> TEST_ISSUE fixed | PRODUCTION_DEFECT DEF-n -> /fix-bugs | PLAN_ISSUE -> /analyze-story | ENVIRONMENT NOT_RUN, or None}}
- Pending manual validation: {{ACs or None}}

- Defects found: {{DEF-1: failing test and evidence, or None}}
- Story Validation: {{PASS | FAIL}}
- Status: {{COMPLETE | BUG_FOUND}}

## {{timestamp}} - /fix-bugs - {{STAGE_PASSED | STAGE_FAILED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION}}
#### {{DEFECT_ID}} - {{FIXED | NOT_FIXED | EXPECTED_BEHAVIOR | NEEDS_INFO | OUT_OF_SCOPE}}
- Triage: {{CONFIRMED_BUG, or why not (cite the AC)}}
- Root cause: {{the cause}}. Why the fix is correct: {{reason}}. Could also affect: {{areas or None}}.
- Fix: {{production files changed, or None}}
- Regression test: {{test name - failed before, passes after | NOT_AVAILABLE (reason)}}
- Sibling occurrences: {{file:line - fixed (in scope) / reported only, or None found}}

- Status: {{BUG_FIXED | BUG_FOUND}}
