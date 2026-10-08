---
name: implement-story
description: "Primary SDLC stage. Implement an already-analyzed story exactly as the story and finalized plan ask, using the smallest required context. Reads work.json first, then only the exact files it names, changes source code only, runs only the tech-stack build/compile check (no unit tests are written or run here), and appends to log.md. Runs as an isolated forked skill. Use after /analyze-story; always followed by /unit-testing."
argument-hint: "<STORY-ID> approved"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Story Implementation

Complete authoritative procedure for the `/implement-story` SDLC stage. Developers invoke this
skill directly; there is no prompt wrapper or custom agent.

## Purpose
Implement the approved story — exactly what the story and the finalized `plan.md` /
`work.json` ask for — using the smallest required context. This stage does **not** test the
story: writing unit tests, running them, and verifying the acceptance criteria all happen in the
next stage, `/unit-testing`.

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context (including the `/analyze-story` run). Cross-stage
  state comes only from persistent `.sdlc/` artifacts, with `work.json` as the authoritative
  scope.
- Write every required `.sdlc` artifact before returning; nothing else survives the fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.

## Role
Developer: implement the exact approved/cached scope with minimal rereading, match existing
architecture, naming, and patterns, and confirm the touched slice builds.
- Keep changes within the cached story scope; no unrelated refactoring.
- Production source changes only. Do **not** create, edit, or delete unit-test files, and do
  **not** run unit tests, test suites, coverage, or manual acceptance checks in this stage.
- Permitted capabilities: `read`, `search`, `edit` (production source and `.sdlc` artifacts),
  and `execute` (the build/compile/type-check command for the repository's tech stack only).
- Do not reread external trackers, full standards documents, BRDs, or broad repository
  documentation when the cache is sufficient, and do not rescan beyond the touched slice unless
  the cache is wrong.

## Required Input
- Story ID
- The approval word `approved` after the Story ID, matched case-insensitively (`approved`,
  `APPROVED`, `Approved`), for example `/implement-story STORY-12 approved`. It is the
  developer's plan approval. Any other word, or no word, is not approval. Do not infer
  approval from chat, `plan.md`, or an earlier run.

## Prerequisites
The story cache must exist:
- `.sdlc/work/<STORY-ID>/work.json`
- `.sdlc/work/<STORY-ID>/plan.md`
- `.sdlc/work/<STORY-ID>/log.md`

If these files are missing, STOP and recommend `/analyze-story`.

## Plan Approval Gate
Run these checks, in order, after reading `work.json` and before reading any source file or
changing anything. If a check fails, change no source code, append a `log.md` entry, and STOP
with `WAITING_FOR_HUMAN` (see Stop Condition).
1. **Approval word:** `approved` (case-insensitive) must follow the Story ID. If missing, STOP.
2. **All questions answered:** every entry in `work.json` → `unresolved_questions` must have a
   non-empty `answer`. If any is empty, STOP and name the unanswered question ids.
3. **Answers stay within the planned scope:** for each answer, check whether it adds a new
   requirement, adds or changes an acceptance criterion, or needs source files outside
   `exact_source_files[].path` / `adjacent_dependencies[].path`. If any answer does, STOP, name the answer
   and why it changes scope, and recommend `/analyze-story <STORY-ID>` to re-plan
   (`/unit-testing` verifies only the criteria recorded in `work.json`, so a new requirement
   must enter through analysis). Clarifying answers that narrow or confirm existing criteria
   pass this check.
4. **Record the approval** (all checks passed): remove each answered question from
   `unresolved_questions` and record it once in `constraints` as
   `"<Q id>: <question in a few words> → <answer>"` (for example
   `"Q2: status when over 10 tags → 400 Bad Request"`); set `plan_review` to `status: APPROVED`, `approved_via: "<the exact
   command invoked>"`, `date: <today>`; set the `log.md` status header Plan Review =
   `APPROVED`. Then continue with the Procedure.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Stage Outputs, Repository Modes, exclusions) and the applicable `.github/instructions/*.instructions.md`.
Do not duplicate those rules here.

## Procedure
1. Read `work.json` and apply the Plan Approval Gate. Then read, in this order:
   - `work.json`
   - `log.md`
   - exact source files (`exact_source_files[].path`; skip `create` entries that do not exist
     yet) and tests (`exact_test_files[].path`) listed in `work.json`
   Do not read `plan.md`: `work.json` already holds the per-file changes,
   `implementation_steps`, `boundaries`, `assumptions`, `risks`, and the answered questions.
2. If an exact file is missing from the cache but the slice is otherwise clear, patch only the
   missing cache field from the smallest nearby read, then continue.
3. Implement the change directly in this forked context, using only the minimal story cache,
   the relevant instructions, and the exact touched files:
   - follow `work.json` → `implementation_steps` in order
   - for each `exact_source_files` entry, apply its `action` (`create` / `modify` / `delete`)
     and make the `change` it describes at its `anchor`
   - respect `boundaries.out_of_scope`, `assumptions`, `constraints` (including developer
     answers), and `contract_changes` — make no contract or data change marked `None`
   - if the change actually made to a file differs from its planned `change`, update that
     entry's `change` in `work.json` and note the difference in `log.md`
4. Run only the narrowest `build_commands` recorded in `work.json` (the build, compile, or
   type-check appropriate to the repository's tech stack — for example the project/module build
   for a compiled language, or the type-check/lint-build for an interpreted one). Do not run a
   full repository build when a focused one exists. Never run unit tests here. If no build
   step exists for the stack, record `NOT_CONFIGURED`.
   - If the build fails because of this change, fix the compile error and rebuild (this is part
     of implementing, not bug fixing). If it still fails, return `STAGE_FAILED` with the error.
5. Set `work.json` → `test_fix_loop` to `fix_iteration: 0`, `status: NOT_STARTED`,
   `open_bugs: []`, and `work.json` → `code_review` to `review_fix_iteration: 0`,
   `status: NOT_STARTED`, `findings: []` (fresh implementation starts fresh test → fix and
   review → fix loops).
6. Update `.sdlc/work/<STORY-ID>/log.md`: set the status header (Implementation =
   status, Current Stage = `UNIT_TESTING` pending) and append an entry with the changed files,
   the build result, `Unit tests: NOT_RUN (not run in this stage)`, and the next recommended
   command.
7. Update `work.json` only if the actual touched files, per-file changes, or build commands
   changed during implementation. Do not update `plan.md`; it stays the approved plan.

## Outputs
- Production source code changes (no test files)
- `.sdlc/work/<STORY-ID>/log.md`
- `work.json` → `test_fix_loop` and `code_review` reset
- Optional cache corrections in `work.json`

## Stop Condition
STOP after implementation and the build check. Do not invoke `/unit-testing`; always recommend
it, because the story is not verified until unit testing passes. A human reviews the changes and
invokes the next skill.

```
CURRENT STAGE: Story Implementation
STATUS: STAGE_PASSED | STAGE_FAILED | WAITING_FOR_HUMAN | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <what was implemented per the plan>; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
NEXT RECOMMENDED COMMAND: /unit-testing <STORY-ID>
```

- `STAGE_FAILED` (build still broken): `NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID> approved — after the build error in SUMMARY is addressed`
- `BLOCKED_MISSING_INFORMATION` (no story cache): `NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID>`

Plan Approval Gate failed (no source code changed):
```
CURRENT STAGE: Story Implementation
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: log.md
SUMMARY: <Plan not approved: "approved" was not given | Unanswered questions: <Q ids> | Answer <Q id> changes scope: <reason>>
NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID> approved — after reviewing plan.md and work.json | /implement-story <STORY-ID> approved — after answering <Q ids> in work.json → unresolved_questions[].answer | /analyze-story <STORY-ID> — re-plan for the scope change, then /implement-story <STORY-ID> approved
```
