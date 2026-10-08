---
name: unit-testing
description: "Primary SDLC stage. Verify that the implementation meets the story's acceptance criteria by writing/updating unit tests and running them, plus scoped coverage. Failures caused by production code are recorded as bugs in work.json and drive the bounded test → fix loop (max 3 fix iterations, then escalate to a developer). When all tests pass, the next stage is /code-review. Runs after /implement-story or /fix-bugs on an existing work folder, or standalone (creating a testcase folder) when no folder exists for the id. Writes unit tests only and runs as an isolated forked skill."
argument-hint: "<WORK-ID> [target behavior] [scope anchor]"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Unit Testing

Complete authoritative procedure for the `/unit-testing` SDLC stage. Developers invoke this
skill directly; there is no prompt wrapper or custom agent.

## Purpose
This is the verification stage of the pipeline. It:
1. checks whether the implementation meets each acceptance criterion in `work.json`,
2. writes or updates unit tests that prove those criteria (success, failure, edge, and branch
   paths) and runs them, plus scoped coverage,
3. records every failure caused by production code as a bug, and decides the next step of the
   bounded test → fix loop (see `.github/copilot-instructions.md` → Story Flow and Test → Fix
   Loop).

It runs after `/implement-story`, after every `/fix-bugs` (bug or review fix), or standalone on
existing code.

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context. Cross-stage state comes only from persistent
  `.sdlc/` artifacts (`.sdlc/context/*`, `.sdlc/work/<ID>/*`), with `work.json` as the
  authoritative scope.
- Write every required `.sdlc` artifact before returning; nothing else survives the fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.

## Role
Tester: prove or disprove the implementation against the acceptance criteria with unit tests.
- Unit tests only, not integration or UI tests; stay within the work scope with no unrelated
  refactoring.
- **Never fix production code in this stage.** Do not change production behavior to make a test
  pass. Only minimal, behavior-neutral testability seams are allowed, and each must be recorded in
  `log.md`. Production defects go to `open_bugs` for `/fix-bugs`.
- If a test fails because the test itself is wrong (bad setup, wrong expectation versus the
  acceptance criterion), correct the test — that is test authoring, not a bug.
- Permitted capabilities: `read`, `search`, `edit` (test files and `.sdlc` artifacts), and
  `execute` (build plus the narrowest unit-test and coverage commands).
- Record `NOT_CONFIGURED` instead of fabricating coverage metrics when tooling is absent.

## Required Input
- Work ID

When the work folder exists, the target behavior is `work.json` → `acceptance_criteria` (with
`planned_tests` as the starting test list) and the scope is `exact_source_files`; do not ask the
user for them.

For standalone work (no work folder), also ask for:
- Target behavior to cover with unit tests
- At least one scope anchor: source file path, class name, function or method name, module /
  feature / service name, existing changed file, or existing test file

## Optional Input
- Test file hint
- Narrow unit-test command when the user already knows it

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately. STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide them
after the stage asks.

## Prerequisites
`.sdlc/context/manifest.json` should exist. If it is missing, STOP and recommend
`/setup-repo-context` first.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Work IDs and Work Folders, Story Flow and Test → Fix Loop,
Next Command Rules, Repository Modes, exclusions) and the applicable
`.github/instructions/*.instructions.md`. Do not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Resolve the work folder `.sdlc/work/<WORK-ID>/`. When it exists, read `work.json` and
   `log.md` before rereading repository context, and read `test_fix_loop` to know the current
   `fix_iteration` and the bugs that the last `/fix-bugs` claims to have fixed.
3. Standalone (no work folder): create `work.json`, `plan.md`, and `log.md` once from
   `.sdlc/templates/`, with `work_type: testcase`, the target behavior as
   `acceptance_criteria`, `test_fix_loop` at `fix_iteration: 0`, and
   `plan_review.status: NOT_REQUIRED` (standalone testcase work has no story plan to approve).
   Write the target files as `exact_source_files` objects (`action: modify`,
   `change: "None — test coverage only"`, `anchor` = the scope anchor, `ac`), the planned tests
   as `planned_tests`, and render `plan.md` from the template with `None` for sections that do
   not apply. `plan.md` is not updated after this.
4. Deterministic test pre-pass:
   - start from `work.json` → `planned_tests`: each planned test must be written; add any
     missing failure, edge, or branch test, and record added tests in `planned_tests`
   - map each acceptance criterion to the source unit(s) that implement it, using
     `exact_source_files[].ac`
   - identify the existing unit test file for that slice (edit it) or plan a new one using the
     repository's test naming and placement pattern
   - identify the narrowest `unit_test_commands` and `coverage_commands`; record them in
     `work.json` if they were `TO_BE_CONFIGURED`
   - check that every test project this run executes or changes has both a unit-test command
     and a matching coverage command. If one is missing (for example a second test project was
     planned without coverage), add it to `work.json` yourself and note the correction in
     `log.md`. Never ask the developer to edit commands.
5. Write or update unit tests so that **every acceptance criterion has at least one test**, plus
   relevant failure, edge, and branch paths. On a retest, extend the existing tests instead of
   rewriting them:
   - after a bug fix, make sure each previously open bug has a regression test
   - after a review fix (`test_fix_loop.status: RETEST_REQUIRED`), rerun the existing tests and
     add tests only for branches the refactor introduced
   - fix every `code_review.findings` entry in a test file that the developer approved (status
     `APPROVED_FOR_FIX`), following its `suggested_fix` (for example splitting a long test class
     or removing a helper with too many parameters), and set it to `FIX_APPLIED`. Do not touch
     findings that are `OPEN` or `WAIVED`, and do not change `review_fix_iteration` (`/fix-bugs`
     counts the round).
6. Build if needed, then run the narrowest `unit_test_commands`. Do not run the whole suite when
   a filtered command exists.
7. Classify every failing test:
   - test defect → fix the test and rerun (does not count as a bug)
   - production defect → bug
8. Evaluate each acceptance criterion as `MET` (covered by passing tests), `NOT_MET` (a bug), or
   `NOT_VERIFIABLE` by unit tests (explain why; this is not a bug, but list it in `SUMMARY`).
9. Run the narrowest `coverage_commands` when coverage tooling exists; otherwise record
   `NOT_CONFIGURED`.
10. Update `work.json` → `test_fix_loop`:
    - no bugs → `status: TESTS_PASSED`, `open_bugs: []`
    - bugs and `fix_iteration` < `max_fix_iterations` (3) → `status: BUGS_OPEN`, replace
      `open_bugs` with the current bugs (`id` = `<WORK-ID>-B<n>`, AC id, failing test, observed,
      expected, suspected file)
    - bugs and `fix_iteration` ≥ 3 → `status: ESCALATED_TO_DEVELOPER`, keep `open_bugs` filled
      for the developer
11. Update `.sdlc/work/<ID>/log.md`: set the status header (Unit Testing status, Test → Fix Loop
    `<fix_iteration>/3 — <status>`, Current Stage `CODE_REVIEW` / `BUG_FIX` /
    `ESCALATED_TO_DEVELOPER`) and append an entry with the changed test files, unit-test
    results, per-criterion results (each AC with its planned tests → pass / fail), coverage,
    bugs, and the next recommended command.
12. Update `work.json` only if the actual touched test files, planned tests, or commands
    changed. Do not update `plan.md`.

## Outputs
- Unit-test file changes (and any recorded behavior-neutral testability seam)
- `.sdlc/work/<ID>/log.md`
- `work.json` → `test_fix_loop`, statuses of approved test-file review findings it fixed, plus
  optional cache corrections in `work.json`
- For standalone work: the new work folder (`work.json`, `plan.md`, `log.md`)

## Stop Condition
STOP after the tests have run and the loop state is recorded. Do not invoke `/fix-bugs` or any
other skill; recommend the next command. A human reviews and invokes it.

All tests pass, all criteria met:
```
CURRENT STAGE: Unit Testing
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <n> tests passed; AC: <AC1 MET, AC2 MET, …>; Coverage: <result | NOT_CONFIGURED>
TEST → FIX LOOP: <fix_iteration>/3 — TESTS_PASSED
NEXT RECOMMENDED COMMAND: /code-review <WORK-ID>
```

Bugs found, loop budget left (`fix_iteration` < 3):
```
CURRENT STAGE: Unit Testing
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <list>
SUMMARY: <passed>/<total> tests passed; AC: <…>; Bugs: <bug ids with one-line cause>
TEST → FIX LOOP: <fix_iteration>/3 — BUGS_OPEN
NEXT RECOMMENDED COMMAND: /fix-bugs <WORK-ID>
```

Bugs remain after the 3rd fix (`fix_iteration` = 3):
```
CURRENT STAGE: Unit Testing
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <list>
SUMMARY: Bugs remain after 3 fix iterations: <bug ids with one-line cause>; details in work.json → test_fix_loop.open_bugs
TEST → FIX LOOP: 3/3 — ESCALATED_TO_DEVELOPER
NEXT RECOMMENDED COMMAND: None — escalated to developer; investigate manually, then rerun /unit-testing <WORK-ID>
```

Missing inputs:
```
CURRENT STAGE: Unit Testing
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing required unit-testing inputs>
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID> — once the missing inputs are supplied
```
