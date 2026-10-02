---
name: story-validation
description: "Validates an implemented story or bug fix: build and type check, lint, creating or updating unit and component tests from plan.md, targeted, regression, and existing integration tests, coverage when configured, verifying every acceptance criterion by its validation type, failure routing, and honest result reporting. Use for /unit-testing."
user-invocable: false
---

# Story Validation

1. Set status `UNIT_TESTING_IN_PROGRESS`. From `plan.md` use only: Acceptance Criteria, Impacted Files,
   `Contracts` and `Behavior Rules` (expected results; never execute Steps), Validation Plan, Unit
   Test Plan, AC-to-Validation Mapping. Read changed code from `files_changed`. Do not redo analysis.
   A bug folder without `plan.md`: each defect's `expected` behavior is its AC and its regression test
   is the planned test.
2. **Build.** Run `build`, then `type_check` if configured, from `work.json`.
   - Commands marked `NOT_RUN (unverified)` are verified by this first use. If one fails because the
     command or working directory is wrong (not because of the code), correct it from the project or
     build files (one correction per command) and rerun.
   - A command that is `NOT_CONFIGURED` because the project is new (GREENFIELD): use the standard
     command for the project files the implementation created (for example the new project or
     package file).
   - Record every command verified, corrected, or discovered in this run in `work.json` and the log
     entry, and recommend `/refresh-repo-context` to cache it. The same applies to test, lint, and
     coverage commands in step 4.
3. **Tests.** Create or update tests from the Unit Test Plan and the `UNIT_TEST` and
   `UI_COMPONENT_TEST` mapping rows, following `.github/instructions/tests.instructions.md` (if no
   nearby test exists, use the representative test in profile section 7). Cover every planned AC,
   every `Behavior Rules` line, changed behavior, important edge cases, and relevant regressions;
   assert the exact `Contracts` values. On re-validation keep existing tests (including `/fix-bugs`
   regression tests) and add only what is missing.
4. **Run** `unit_test`, then `regression_test`, then `integration_test` (only for
   `INTEGRATION_FUNCTIONAL` rows, when configured), then `lint` if the Validation Plan requires it.
5. **Route every failure** (never fix one category as another):

   | Category | Examples | Action |
   |---|---|---|
   | `TEST_ISSUE` | wrong test setup, mock, fixture, data, typo | Fix here; 3 attempts in total, then `STAGE_FAILED` with the exact failure |
   | `PRODUCTION_DEFECT` | production code does not compile; a correct test fails; lint finding in production code | Never touch production code. Add `DEF-n` to `defects` (description, evidence, AC broken, `OPEN`) |
   | `PLAN_ISSUE` | AC contradictory or not validatable; Contracts or Behavior Rules conflict with an AC; wrong expected result; behavior or file missing from the plan | Change neither tests nor code to fit; `waiting_for_human` = `PLAN_ISSUE: <what>` |
   | `ENVIRONMENT` | missing SDK, required service down | `NOT_RUN (reason)`; set `waiting_for_human` |

6. **Validate each AC** by type: `UNIT_TEST` / `UI_COMPONENT_TEST` → `PASS` only if the mapped test ran
   and passed; `BUILD_CHECK` → `PASS` only if step 2 passed; `INTEGRATION_FUNCTIONAL` → `PASS` only if
   the existing suite passed, else `NOT_VERIFIED (manual validation required)` and list it as pending
   manual validation; anything else unproven → `NOT_VERIFIED (reason)`.
7. **Coverage**, only if tooling exists: `coverage` scoped to changed files vs `coverage_goal`; report
   shortfalls as gaps. Never install tooling; otherwise `NOT_CONFIGURED`.
8. **Result** (first match wins):
   - any `PLAN_ISSUE` → `Story Validation: FAIL`, status unchanged, next `/analyze-story <ID>`
   - any `PRODUCTION_DEFECT` → `Story Validation: FAIL`, status `BUG_FOUND`, next `/fix-bugs <ID>`
   - unresolved `TEST_ISSUE`, `ENVIRONMENT`, or an automatable AC still `NOT_VERIFIED` →
     `Story Validation: FAIL`, status unchanged, `waiting_for_human` says why
   - otherwise → `Story Validation: PASS`, status `COMPLETE`; list ACs pending manual validation
9. **Record.** Append the `/unit-testing` entry from `.sdlc/templates/log.template.md`; update
   `files_changed.tests`, status, and `waiting_for_human`.

**Standalone test task** (target with no work folder): create `.sdlc/work/TEST-<target>/work.json`
from the template (`work_type` = `testcase`, status `IMPLEMENTATION_COMPLETE`, `scope_anchors` = the
target, `commands` from `manifest.json`, `coverage_goal` from profile section 7) and `log.md` with only
`# Log - <ID>`; list public behaviors in `behaviors_to_cover` as `B1 - ...` and use them as ACs
(type `UNIT_TEST`).
