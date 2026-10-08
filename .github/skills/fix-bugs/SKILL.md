---
name: fix-bugs
description: "Primary SDLC stage. Fix defects in the smallest possible code slice — the open bugs recorded by /unit-testing in work.json (test → fix loop, max 3 iterations), the blocking findings recorded by /code-review (review → fix loop, max 2 iterations), or a reported bug (standalone, when no work folder exists for the id). Also records the developer's review decisions given in the command (approve / waive findings). Fixes production code only, runs only the tech-stack build (no unit tests; verification happens in /unit-testing), increments the loop counter, and runs as an isolated forked skill. Always followed by /unit-testing."
argument-hint: "<WORK-ID | BUG-ID> [approve <R ids> | waive <R ids> \"<reason>\"] [description] [scoping hint]"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Bug Fix

Complete authoritative procedure for the `/fix-bugs` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Fix defects in the smallest possible code slice. Sources:
- **Open bugs**: the bugs that `/unit-testing` recorded in `work.json` →
  `test_fix_loop.open_bugs`, as one iteration of the bounded test → fix loop (see
  `.github/copilot-instructions.md` → Story Flow and Test → Fix Loop).
- **Review findings**: the blocking findings that `/code-review` recorded in `work.json` →
  `code_review.findings`, plus any finding the developer approved (including MINOR ones), as one
  iteration of the bounded review → fix loop (see `.github/copilot-instructions.md` → Review →
  Fix Loop).
- **Standalone bug**: a reported bug with no work folder yet (see Work IDs and Work Folders in
  `.github/copilot-instructions.md`), in a repository that already has implemented code.

This stage does not test. The fix is verified by the next `/unit-testing` run.

### Review Decisions in the Command
The developer decides on review findings by typing the decision in the command, never by
editing `work.json`:
- `/fix-bugs <WORK-ID> approve R1 [R3 …]` — fix those findings in this round:
  - a blocking finding that needs a decision (`auto_fixable: false`): the pipeline may make the
    contract or behavior change it describes
  - a `MINOR` finding: the developer opts in to fixing it now, so it doesn't come back as a PR
    comment. It is fixed in the same round as the blocking findings, at no extra round cost.
- `/fix-bugs <WORK-ID> waive R1 [R3 …] "<reason>"` — keep the code as it is. A reason is
  required.

Finding ids may be given short (`R1`) or full (`<WORK-ID>-R1`), separated by spaces or commas
(`approve R3 R9` or `approve R3,R9`). Record the decisions first (see Recording Review
Decisions), then continue with the Input Source Selection.

### Input Source Selection
Decide the source before fixing anything, in this order:
1. No folder `.sdlc/work/<ID>/` exists → **standalone bug fix** of the reported bug.
2. `test_fix_loop.status` is `BUGS_OPEN` → **bug fix** (open bugs always come first).
3. `code_review.findings` has at least one finding to fix → **review fix**. A finding is to be
   fixed when it is `BLOCKER` / `MAJOR` with status `OPEN` and `auto_fixable: true`, or it has
   status `APPROVED_FOR_FIX` (any severity). Never fix a `WAIVED` finding, an unapproved `MINOR`
   finding, or an `OPEN` finding with `auto_fixable: false` (it waits for the developer's
   decision).
4. Otherwise → nothing to fix: STOP with `BLOCKED_MISSING_INFORMATION`.

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

## Required Input
- Work ID or Bug ID

When the work folder exists, the bug descriptions and anchors come from `work.json` →
`test_fix_loop.open_bugs`, and for a review fix from `work.json` → `code_review.findings`; do
not ask the user for them. Review decisions come only from the `approve` / `waive` words in the
command (see Review Decisions in the Command).

For a standalone bug (no work folder), also ask for:
- Bug description
- At least one scoping hint: suspected file, module / feature / service name, endpoint /
  screen, failing test name, stack trace location, or reproduction steps. If the description is
  too vague to identify one concrete anchor, ask for a scoping hint instead of exploring
  broadly.

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately. STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide them
after the stage asks.

## Prerequisites
- `.sdlc/context/manifest.json` should exist. If it is missing, STOP and recommend
  `/setup-repo-context` first.
- With an existing work folder, the Input Source Selection must find open bugs or review
  findings to fix. If it finds neither, STOP with `BLOCKED_MISSING_INFORMATION` and recommend
  `/unit-testing <WORK-ID>` (or `/code-review <WORK-ID>` when tests have passed).
- **Loop limit guard (bug fix):** if `test_fix_loop.fix_iteration` ≥ `max_fix_iterations` (3),
  do not fix anything. Set `status: ESCALATED_TO_DEVELOPER`, log it, and STOP with
  `WAITING_FOR_HUMAN`.
- **Loop limit guard (review fix):** if `code_review.review_fix_iteration` ≥
  `max_review_fix_iterations` (2), do not fix anything. Set `code_review.status:
  ESCALATED_TO_DEVELOPER`, log it, and STOP with `WAITING_FOR_HUMAN`. Review decisions in the
  command are still recorded at the limit, because recording them changes no code.

## Recording Review Decisions
When the command contains `approve` or `waive`, before any fixing:
1. Each id must exist in `code_review.findings` with status `OPEN` or `APPROVED_FOR_FIX` (any
   severity). `waive` must have a non-empty quoted reason. If an id is unknown or the reason is missing,
   change nothing, append a `log.md` entry, and STOP with `BLOCKED_MISSING_INFORMATION`,
   naming the problem and the valid open finding ids.
2. `approve` → set each finding's `status: APPROVED_FOR_FIX`. `waive` → set
   `status: WAIVED` and `waiver_reason: "<reason>"`. On each, set `decided_via` to the exact
   command invoked.
3. Append a `log.md` entry listing each decision.
4. If no finding is left to fix (see Input Source Selection, item 3) and no open bug remains,
   there is nothing to fix: STOP with `STAGE_PASSED` and recommend `/code-review <WORK-ID>`,
   which confirms the result. Otherwise continue with the Input Source Selection; a blocking
   `OPEN` finding with `auto_fixable: false` that is still undecided is left untouched.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Work IDs and Work Folders, Story Flow and Test → Fix Loop,
Review → Fix Loop, Next Command Rules, Repository Modes, exclusions) and the applicable
`.github/instructions/*.instructions.md`. Do not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules. On `fix_iteration` 2 or 3, escalate the
   effort mode one step (`low` → `standard` → `deep`): an earlier fix did not hold.
2. Resolve the work folder `.sdlc/work/<ID>/`. When it exists, read `work.json` and `log.md`,
   record any review decisions from the command (Recording Review Decisions), then apply the
   Input Source Selection, the Prerequisites, and the loop limit guards. For a **review fix**,
   continue with the Review Fix Procedure below instead of steps 3–10.
3. Standalone (no work folder): create `work.json`, `plan.md`, and `log.md` once from
   `.sdlc/templates/`, with `work_type: bug`, the reported bug as the single `open_bugs` entry
   and its expected behavior as `acceptance_criteria`, `fix_iteration: 0`, and
   `plan_review.status: NOT_REQUIRED` (standalone bug work has no story plan to approve).
   Write the files to fix as `exact_source_files` objects (`path`, `action`, `anchor`,
   `change` = the planned fix, `ac`, `reason` = the anchor that points there), plus `risks`
   and `contract_changes`, and render `plan.md` from the template with `None` for sections
   that do not apply. `plan.md` is not updated after this.
4. Deterministic bug pre-pass for each open bug:
   - take the most concrete anchor (failing test → code under test → suspected file)
   - identify the smallest exact source-file set needed for the fix
   - identify the narrowest `build_commands`
   - record any missing facts in the compact cache instead of broad-reading immediately
   - on iterations 2–3, read the earlier Bug Fix log entries so the same failed fix is not
     repeated
5. When a bug is ambiguous, risky, or blocked, record it in `work.json` → `risks` /
   `unresolved_questions`.
6. Fix every open bug at its root cause, using only the compact work cache, the relevant
   instructions, and the exact files identified by the cache or the bug anchor.
7. Run only the narrowest `build_commands` for the repository's tech stack (`NOT_CONFIGURED`
   when no build step exists). Fix any compile error introduced by the fix. Never run unit
   tests here.
8. Update `work.json` → `test_fix_loop`: increment `fix_iteration` by 1 and keep `open_bugs`
   (marked as "fix applied, pending retest") so `/unit-testing` knows what to regression-test.
9. Update `.sdlc/work/<ID>/log.md`: set the status header (Bug Fix status, Test → Fix Loop
   `<fix_iteration>/3`, Current Stage `UNIT_TESTING`) and append an entry with the bugs
   addressed, root cause per bug, changed files, build result, `Unit tests: NOT_RUN (verified
   in /unit-testing)`, and the next recommended command.
10. Update `work.json` only if the actual touched files, anchors, or build commands changed
    during bug fixing: add or update the `exact_source_files` entry for each file touched (its
    `change` describes the fix).

## Review Fix Procedure
When the Input Source Selection chose **review fix**, run steps 1–2 of the Procedure, then:
1. Read `standards/code-review-standards.md` only for the rule ids of the findings being fixed.
   On `review_fix_iteration` 1, read the earlier Bug Fix log entry so the same failed fix is not
   repeated.
2. Fix each selected finding in a production file at its `file` / `anchor`, following its
   `suggested_fix` and rule:
   - **behavior-preserving by default**: extract methods, flatten nesting with guard clauses,
     remove duplication or dead code, correct null handling, replace magic values with named
     constants, dispose resources, and similar refactorings. Do not change what the code does.
   - only an `APPROVED_FOR_FIX` finding may change a public contract, a signature used by
     `adjacent_dependencies`, or behavior (for example handling a race condition), as its
     `suggested_fix` describes. It must never change behavior an acceptance criterion defines:
     if it would, leave the finding `APPROVED_FOR_FIX`, change nothing for it, and say in
     `SUMMARY` that it needs `/analyze-story` to re-plan.
   - stay within `exact_source_files`; a new file is allowed only when the fix needs one inside
     `boundaries.primary_slice` (for example a request object), recorded as a new
     `exact_source_files` entry with `action: create`
   - findings in test files are not fixed here: leave them `APPROVED_FOR_FIX` (or `OPEN`); the
     next `/unit-testing` fixes the approved ones
3. When any production file changed, run only the narrowest `build_commands` (and the recorded
   `code_review.analyzer_commands` when they are part of the build). Fix any compile error
   introduced by the fix. Never run unit tests here.
4. Update `work.json`: each fixed finding → `status: FIX_APPLIED`; increment
   `code_review.review_fix_iteration` by 1 (once per run, however many findings it covers,
   including a run that only hands approved test-file findings to `/unit-testing`); set
   `code_review.status: FIXES_APPLIED` and `test_fix_loop.status: RETEST_REQUIRED` (the change
   must be re-verified before re-review). Update `exact_source_files` entries for any file
   touched.
5. Update `.sdlc/work/<ID>/log.md`: set the status header (Bug Fix status, Review → Fix Loop
   `<review_fix_iteration>/2`, Current Stage `UNIT_TESTING`) and append an entry with the
   findings addressed (id and rule), what was changed for each, changed files, build result,
   `Unit tests: NOT_RUN (verified in /unit-testing)`, and the next recommended command.

## Outputs
- Production source fixes (no test files)
- `.sdlc/work/<ID>/log.md`
- `work.json` → `test_fix_loop.fix_iteration` (bug fix) or `code_review` finding statuses,
  `review_fix_iteration`, and `test_fix_loop.status` (review fix), plus optional cache
  corrections in `work.json`
- For a standalone bug: the new work folder (`work.json`, `plan.md`, `log.md`)
- Review decisions from the command: `code_review.findings[].status`, `waiver_reason`,
  `decided_via`

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
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID>
```

Review findings fixed:
```
CURRENT STAGE: Bug Fix (review findings)
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: Fixed review findings <ids (rule)>: <one-line change each>; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
REVIEW → FIX LOOP: <review_fix_iteration>/2 — fixes applied, pending retest and re-review
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID>
```

Build still broken after the fix:
```
CURRENT STAGE: Bug Fix
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <list>
SUMMARY: <build error>
TEST → FIX LOOP: <fix_iteration>/3
NEXT RECOMMENDED COMMAND: None — developer to resolve the build error, then /unit-testing <WORK-ID>
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

Review loop limit reached:
```
CURRENT STAGE: Bug Fix (review findings)
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: 2 review fix iterations already used; blocking findings <ids> need developer fix or waiver
REVIEW → FIX LOOP: 2/2 — ESCALATED_TO_DEVELOPER
NEXT RECOMMENDED COMMAND: None — escalated to developer
```

Decisions recorded, nothing left to fix:
```
CURRENT STAGE: Bug Fix (review decisions)
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: Recorded decisions: <id → APPROVED_FOR_FIX | WAIVED ("reason")>; no code changed
REVIEW → FIX LOOP: <review_fix_iteration>/2
NEXT RECOMMENDED COMMAND: /code-review <WORK-ID>
```

Missing inputs or nothing to fix:
```
CURRENT STAGE: Bug Fix
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing bug-fix inputs | no open bugs or fixable review findings in work.json | unknown finding id <id> (open findings: <ids>) | waive needs a reason in quotes>
NEXT RECOMMENDED COMMAND: /fix-bugs <ID> once inputs are supplied | /unit-testing <WORK-ID> when there are no open bugs | /code-review <WORK-ID> when tests have passed
```
