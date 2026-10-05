---
name: unit-testing
description: "Primary SDLC stage. Verify that the implementation meets the story's acceptance criteria by writing/updating unit tests and running them, plus scoped coverage. Failures caused by production code are recorded as bugs in work.json and drive the bounded test → fix loop (max 3 fix iterations, then escalate to a developer). Supports continuation after /implement-story or /fix-bugs, or standalone test work (relationship modes: standalone, current_story, existing_work_id). Writes unit tests only and runs as an isolated forked skill."
argument-hint: "<WORK-ID> <mode: standalone|current_story|existing_work_id> [target behavior] [scope anchor]"
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

It runs after `/implement-story`, after every `/fix-bugs`, or standalone on existing code.

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
- Do not reread external trackers, full standards documents, BRDs, or broad repository
  documentation when the cache is sufficient, and do not broad-scan when a concrete anchor
  exists.

## Required Input
- Work ID
- Relationship mode: `standalone`, `current_story`, or `existing_work_id` (with the
  `existing_work_id`)

When attached to an existing work folder (`current_story` / `existing_work_id`), the target
behavior is `work.json` → `acceptance_criteria` and the scope anchors are `exact_source_files` /
`scope_anchors`; do not ask the user for them.

For `standalone` work with no work folder, also ask for:
- Target behavior to cover with unit tests
- At least one scope anchor: source file path, class name, function or method name, module /
  feature / service name, existing changed file, or existing test file

## Optional Input
- Test file hint
- Coverage goal for the requested scope
- Narrow unit-test command when the user already knows it

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately. STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide them
after the stage asks.

## Prerequisites
`.sdlc/context/manifest.json` should exist. If it is missing, STOP and recommend
`/setup-repo-context` first.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Story Flow and Test → Fix Loop, Next Command Rules,
Repository Modes, exclusions) and the applicable `.github/instructions/*.instructions.md`. Do
not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Resolve the work folder:
   - `current_story` → the current in-progress story work folder
   - `existing_work_id` → that explicit work folder
   - `standalone` → `.sdlc/work/<WORK-ID>/`
3. Read `work.json` and `log.md` (and `plan.md` only for missing intent, boundaries, or the
   verification note) before rereading repository context. Read `test_fix_loop` to know the
   current `fix_iteration` and the bugs that the last `/fix-bugs` claims to have fixed. Also
   read `work.json` → `review.return_after_testing`: when it is `true`, this run verifies a
   review-fix cycle (see the Stop Condition routing), not fresh implementation.
4. For a standalone folder that does not exist yet, create `work.json`, `plan.md`, and `log.md`
   once from `.sdlc/templates/`, with the target behavior as `acceptance_criteria` and
   `test_fix_loop` at `fix_iteration: 0`.
5. Deterministic test pre-pass:
   - map each acceptance criterion to the source unit(s) that implement it
   - identify the existing unit test file for that slice (edit it) or plan a new one using the
     repository's test naming and placement pattern
   - identify the narrowest `unit_test_commands` and `coverage_commands`; record them in
     `work.json` if they were `TO_BE_CONFIGURED`
   - select applicable standard ids from `.sdlc/context/manifest.json` → `standards.items`
6. Write or update unit tests so that **every acceptance criterion has at least one test**, plus
   relevant failure, edge, and branch paths. After a `/fix-bugs` run, make sure each previously
   open bug has a regression test.
7. Build if needed, then run the narrowest `unit_test_commands`. Do not run the whole suite when
   a filtered command exists.
8. Classify every failing test:
   - test defect → fix the test and rerun (does not count as a bug)
   - production defect → bug
9. Evaluate each acceptance criterion as `MET` (covered by passing tests), `NOT_MET` (a bug), or
   `NOT_VERIFIABLE` by unit tests (explain why; this is not a bug, but list it in `SUMMARY`).
10. Run the narrowest `coverage_commands` when coverage tooling exists; otherwise record
    `NOT_CONFIGURED`.
11. Update `work.json` → `test_fix_loop`:
    - no bugs → `status: TESTS_PASSED`, `open_bugs: []`
    - bugs and `fix_iteration` < `max_fix_iterations` (3) → `status: BUGS_OPEN`, replace
      `open_bugs` with the current bugs (`id` = `<WORK-ID>-B<n>`, AC id, failing test, observed,
      expected, suspected file)
    - bugs and `fix_iteration` ≥ 3 → `status: ESCALATED_TO_DEVELOPER`, keep `open_bugs` filled
      for the developer
12. Update `.sdlc/work/<ID>/log.md`: set the status header (Unit Testing status, Test → Fix Loop
    `<fix_iteration>/3 — <status>`, Current Stage `PREPARE_PR` (normal PASS) /
    `PR_REVIEW` (review-fix PASS) / `BUG_FIX` / `ESCALATED_TO_DEVELOPER`) and append an entry
    with the changed test files, unit-test results, per-criterion results, coverage, bugs, and
    the next recommended command.
13. Update `work.json` (and `plan.md` boundaries) only if the actual touched test files or
    commands changed.

## Cost-Control Behavior
- Do not broad-scan the repository when `work.json` or the input already provides a file,
  class, function, module, service, or existing test-file anchor.
- Reuse an existing `work.json` first. Only read markdown work artifacts or shared repository
  context for explicit missing facts.
- For standards, rely on the `selected_standards` ids and the compact
  `.github/instructions/standards/*.instructions.md` files that auto-apply to the touched files;
  read a full `standards/*.md` file only for an exact rule or missing detail, and never reread
  the entire `standards/` folder.
- On a retest after `/fix-bugs`, extend the existing tests instead of rewriting them.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.
- Do not run the entire test suite by default when a single-file, single-class, single-module,
  or filtered unit-test command exists.

## Outputs
- Unit-test file changes (and any recorded behavior-neutral testability seam)
- `.sdlc/work/<ID>/log.md`
- `work.json` → `test_fix_loop`, plus optional cache corrections in `work.json` and `plan.md`

## Stop Condition
STOP after the tests have run and the loop state is recorded. Do not invoke `/fix-bugs` or any
other skill; recommend the next command. A human reviews and invokes it.

**PASS routing** depends on `work.json` → `review.return_after_testing`:
- `false` (normal implementation validation) → recommend `/prepare-pr <ID>`.
- `true` (a `/address-review-comments` fix is being revalidated) → the code must re-enter review
  from L0: STATUS `WAITING_FOR_HUMAN`, the developer updates the SAME PR, then `/l0-review <ID>`.
  Leave `review.return_after_testing` as `true` (the work stays in the PR/review phase). Do not
  recommend `/prepare-pr` in this case.

All tests pass, all criteria met — normal implementation validation (`review.return_after_testing` is `false`):
```
CURRENT STAGE: Unit Testing
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <n> tests passed; AC: <AC1 MET, AC2 MET, …>; Coverage: <result | NOT_CONFIGURED>
TEST → FIX LOOP: <fix_iteration>/3 — TESTS_PASSED
NEXT RECOMMENDED COMMAND: /prepare-pr <ID>
```

All tests pass, all criteria met — review-fix revalidation (`review.return_after_testing` is `true`):
```
CURRENT STAGE: Unit Testing
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <list>
SUMMARY: <n> tests passed after review fixes (origin: <l0|l1>); AC: <…>; Coverage: <result | NOT_CONFIGURED>
TEST → FIX LOOP: <fix_iteration>/3 — TESTS_PASSED
HUMAN ACTION: Update/push the changes to the SAME existing PR
NEXT RECOMMENDED COMMAND: /l0-review <ID> — after the PR is updated (reviewed code re-enters from L0)
```

Bugs found, loop budget left (`fix_iteration` < 3):
```
CURRENT STAGE: Unit Testing
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <list>
SUMMARY: <passed>/<total> tests passed; AC: <…>; Bugs: <bug ids with one-line cause>
TEST → FIX LOOP: <fix_iteration>/3 — BUGS_OPEN
NEXT RECOMMENDED COMMAND: /fix-bugs <WORK-ID> <same relationship mode>
```

Bugs remain after the 3rd fix (`fix_iteration` = 3):
```
CURRENT STAGE: Unit Testing
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <list>
SUMMARY: Bugs remain after 3 fix iterations: <bug ids with one-line cause>; details in work.json → test_fix_loop.open_bugs
TEST → FIX LOOP: 3/3 — ESCALATED_TO_DEVELOPER
NEXT RECOMMENDED COMMAND: None — escalated to developer; investigate manually, then rerun /unit-testing <WORK-ID> <mode>
```

Missing inputs:
```
CURRENT STAGE: Unit Testing
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing required unit-testing inputs>
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID> <mode> — once the missing inputs are supplied
```
