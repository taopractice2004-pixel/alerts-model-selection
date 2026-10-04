---
name: fix-bugs
description: "Primary SDLC stage. Fix defects in the smallest possible code slice — either the open bugs recorded by /unit-testing in work.json (test → fix loop, max 3 iterations) or a reported bug (standalone). Fixes production code only, runs only the tech-stack build (no unit tests; verification happens in /unit-testing), increments the loop counter, and runs as an isolated forked skill. Always followed by /unit-testing."
argument-hint: "<WORK-ID | BUG-ID> <mode: standalone|current_story|existing_work_id> [description] [scoping hint]"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Bug Fix

Complete authoritative procedure for the `/fix-bugs` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Fix defects in the smallest possible code slice as one iteration of the bounded test → fix loop
(see `.github/copilot-instructions.md` → Story Flow and Test → Fix Loop). Sources of bugs:
- **Loop mode** (`current_story` / `existing_work_id`): the bugs that `/unit-testing` recorded in
  `work.json` → `test_fix_loop.open_bugs`.
- **Standalone mode**: a reported bug in a repository that already has implemented code.

This stage does not test. The fix is verified by the next `/unit-testing` run.

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
Bug fixer: diagnose each defect from its most concrete anchor (failing test, suspected file,
stack trace, endpoint, module, or reproduction path), apply the smallest root-cause fix rather
than a workaround, and confirm the slice builds.
- Keep the fix within the bug scope; no unrelated refactoring.
- Production source only. Do **not** create, edit, or delete unit tests, do not weaken or skip
  a failing test, and do **not** run unit tests or coverage in this stage.
- Permitted capabilities: `read`, `search`, `edit` (production source and `.sdlc` artifacts),
  and `execute` (the build/compile/type-check command for the repository's tech stack only).
- Do not reread external trackers, full standards documents, BRDs, or broad repository
  documentation when the cache is sufficient, and do not broad-scan when a concrete anchor
  exists.

## Required Input
- Work ID (loop mode) or Bug ID (standalone)
- Relationship mode: `standalone`, `current_story`, or `existing_work_id` (with the
  `existing_work_id`)

In loop mode the bug descriptions and anchors come from `work.json` → `test_fix_loop.open_bugs`;
do not ask the user for them.

In standalone mode with no work folder, also ask for:
- Bug description
- At least one scoping hint: suspected file, module / feature / service name, endpoint /
  screen, failing test name, stack trace location, or reproduction steps

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately. STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide them
after the stage asks.

## Prerequisites
- `.sdlc/context/manifest.json` should exist. If it is missing, STOP and recommend
  `/setup-repo-context` first.
- Loop mode: `test_fix_loop.status` must be `BUGS_OPEN` with at least one open bug. If there
  are no open bugs, STOP with `BLOCKED_MISSING_INFORMATION` and recommend
  `/unit-testing <WORK-ID> <mode>`.
- **Loop limit guard:** if `test_fix_loop.fix_iteration` ≥ `max_fix_iterations` (3), do not fix
  anything. Set `status: ESCALATED_TO_DEVELOPER`, log it, and STOP with `WAITING_FOR_HUMAN`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Story Flow and Test → Fix Loop, Next Command Rules,
Repository Modes, exclusions) and the applicable `.github/instructions/*.instructions.md`. Do
not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules. On `fix_iteration` 2 or 3, escalate the
   effort mode one step (`low` → `standard` → `deep`): an earlier fix did not hold.
2. Resolve the work folder:
   - `current_story` → the current in-progress story work folder
   - `existing_work_id` → that explicit work folder
   - `standalone` → `.sdlc/work/<BUG-ID>/`
3. Read `work.json` and `log.md` (and `plan.md` only for missing intent or boundaries). Apply
   the Prerequisites and the loop limit guard.
4. Standalone with no work folder: create `work.json`, `plan.md`, and `log.md` once from
   `.sdlc/templates/`, recording the reported bug as the single `open_bugs` entry and its
   expected behavior as `acceptance_criteria`, with `fix_iteration: 0`.
5. Deterministic bug pre-pass for each open bug:
   - take the most concrete anchor (failing test → code under test → suspected file)
   - identify the smallest exact source-file set needed for the fix
   - identify the narrowest `build_commands`
   - select applicable standard ids from `.sdlc/context/manifest.json` → `standards.items`
   - record any missing facts in the compact cache instead of broad-reading immediately
   - on iterations 2–3, read the earlier Bug Fix log entries so the same failed fix is not
     repeated
6. Fill the Requirement Analysis section of `plan.md` only when a bug is ambiguous, risky, or
   blocked.
7. Fix every open bug at its root cause, using only the compact work cache, the relevant
   instructions, and the exact files identified by the cache or the bug anchor.
8. Run only the narrowest `build_commands` for the repository's tech stack (`NOT_CONFIGURED`
   when no build step exists). Fix any compile error introduced by the fix. Never run unit
   tests here.
9. Update `work.json` → `test_fix_loop`: increment `fix_iteration` by 1 and keep `open_bugs`
   (marked as "fix applied, pending retest") so `/unit-testing` knows what to regression-test.
10. Update `.sdlc/work/<ID>/log.md`: set the status header (Bug Fix status, Test → Fix Loop
    `<fix_iteration>/3`, Current Stage `UNIT_TESTING`) and append an entry with the bugs
    addressed, root cause per bug, changed files, build result, `Unit tests: NOT_RUN (verified
    in /unit-testing)`, and the next recommended command.
11. Update `work.json` (and `plan.md` boundaries) only if the actual touched files, anchors, or
    build commands changed during bug fixing.

## Cost-Control Behavior
- Do not broad-scan the repository when the bug already provides a failing test, file, stack
  trace location, endpoint, or module anchor.
- Reuse an existing `work.json` first. Only read markdown work artifacts or shared repository
  context for explicit missing facts.
- For standards, rely on the `selected_standards` ids and the compact
  `.github/instructions/standards/*.instructions.md` files that auto-apply to the touched files;
  read a full `standards/*.md` file only for an exact rule or missing detail, and never reread
  the entire `standards/` folder.
- Read the smallest local slice first.
- If a standalone bug description is too vague to identify one concrete anchor, ask for one
  scoping hint instead of exploring broadly.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.

## Outputs
- Production source fixes (no test files)
- `.sdlc/work/<ID>/log.md`
- `work.json` → `test_fix_loop.fix_iteration`, plus optional cache corrections in `work.json`
  and `plan.md`

## Stop Condition
STOP after the fix and the build check. Do not invoke `/unit-testing`; always recommend it —
the fix is not verified until unit tests pass.

Fix applied:
```
CURRENT STAGE: Bug Fix
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: Fixed <bug ids>: <one-line root cause each>; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
TEST → FIX LOOP: <fix_iteration>/3 — fix applied, pending retest
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID> <same relationship mode>
```

Build still broken after the fix:
```
CURRENT STAGE: Bug Fix
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <list>
SUMMARY: <build error>
TEST → FIX LOOP: <fix_iteration>/3
NEXT RECOMMENDED COMMAND: None — developer to resolve the build error, then /unit-testing <WORK-ID> <mode>
```

Loop limit reached:
```
CURRENT STAGE: Bug Fix
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: 3 fix iterations already used; open bugs <ids> need developer investigation
TEST → FIX LOOP: 3/3 — ESCALATED_TO_DEVELOPER
NEXT RECOMMENDED COMMAND: None — escalated to developer
```

Missing inputs or no open bugs:
```
CURRENT STAGE: Bug Fix
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing bug-fix inputs | no open bugs in work.json>
NEXT RECOMMENDED COMMAND: /fix-bugs <ID> <mode> once inputs are supplied | /unit-testing <WORK-ID> <mode> when there are no open bugs
```
