---
name: implement-story
description: "Primary SDLC stage. Implement an already-analyzed story exactly as the story and finalized plan ask, using the smallest required context. Reads work.json first, then only the exact files it names, changes source code only, runs only the tech-stack build/compile check (no unit tests are written or run here), and appends to log.md. Runs as an isolated forked skill. Use after /analyze-story; always followed by /unit-testing."
argument-hint: "<STORY-ID>"
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
Story ID. If the only in-progress story is not inferable, ask for the Story ID.

## Prerequisites
The story cache must exist:
- `.sdlc/work/<STORY-ID>/work.json`
- `.sdlc/work/<STORY-ID>/plan.md`
- `.sdlc/work/<STORY-ID>/log.md`

If these files are missing, STOP and recommend `/analyze-story`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Stage Outputs, Repository Modes, exclusions) and the applicable `.github/instructions/*.instructions.md`.
Do not duplicate those rules here.

## Procedure
1. Read, in this order:
   - `work.json`
   - `log.md`
   - exact source files and tests listed in `work.json`
2. Read `plan.md` only if implementation intent, validation rationale, out-of-scope boundaries,
   or requirement-analysis risks are missing from `work.json` (always for an `AMBIGUOUS` case
   or when unresolved questions remain).
3. Read `.sdlc/context/project-profile.md` or `.sdlc/context/standards-summary.md` only if
   `repo_context_fallback_needed` is true or the cache explicitly records a missing fact.
   For standards, rely on the compact `.github/instructions/standards/*.instructions.md` files
   that auto-apply to the touched files and on the `selected_standards` ids in the cache; read a
   full `standards/*.md` file only when an exact rule or missing detail is required. Never
   reread the entire `standards/` folder.
4. If an exact file is missing from the cache but the slice is otherwise clear, patch only the
   missing cache field from the smallest nearby read, then continue.
5. Do not broad-scan the repository when the cache already identifies exact files or anchors.
6. Implement the change directly in this forked context, using only the minimal story cache,
   the relevant instructions, and the exact touched files.
7. Run only the narrowest `build_commands` recorded in `work.json` (the build, compile, or
   type-check appropriate to the repository's tech stack — for example the project/module build
   for a compiled language, or the type-check/lint-build for an interpreted one). Do not run a
   full repository build when a focused one exists. Never run unit tests here. If no build
   step exists for the stack, record `NOT_CONFIGURED`.
   - If the build fails because of this change, fix the compile error and rebuild (this is part
     of implementing, not bug fixing). If it still fails, return `STAGE_FAILED` with the error.
8. Set `work.json` → `test_fix_loop` to `fix_iteration: 0`, `status: NOT_STARTED`,
   `open_bugs: []` (fresh implementation starts a fresh test → fix loop).
9. Update `.sdlc/work/<STORY-ID>/log.md`: set the status header (Implementation =
   status, Current Stage = `UNIT_TESTING` pending) and append an entry with the changed files,
   the build result, `Unit tests: NOT_RUN (not run in this stage)`, and the next recommended
   command.
10. Update `work.json` (and `plan.md` boundaries) only if the actual touched files or build
    commands changed during implementation.

## Cost-Control Behavior
- Cache first, then exact files only; do not reread the tracker or broad documentation.
- Do not rescan the repository when the cache already names exact files or anchors.
- For standards, rely on the `selected_standards` ids and the compact
  `.github/instructions/standards/*.instructions.md` files that auto-apply to the touched files;
  read a full `standards/*.md` file only for an exact rule or missing detail, and never reread
  the entire `standards/` folder.
- Run the narrowest build only; never run tests in this stage.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.
- Correct the cache only when the actual touched slice changed.

## Outputs
- Production source code changes (no test files)
- `.sdlc/work/<STORY-ID>/log.md`
- `work.json` → `test_fix_loop` reset
- Optional cache corrections in `work.json` and `plan.md`

## Stop Condition
STOP after implementation and the build check. Do not invoke `/unit-testing`; always recommend
it, because the story is not verified until unit testing passes. A human reviews the changes and
invokes the next skill.

```
CURRENT STAGE: Story Implementation
STATUS: STAGE_PASSED | STAGE_FAILED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <what was implemented per the plan>; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
NEXT RECOMMENDED COMMAND: /unit-testing <STORY-ID> current_story
```

- `STAGE_FAILED` (build still broken): `NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID> — after the build error in SUMMARY is addressed`
- `BLOCKED_MISSING_INFORMATION` (no story cache): `NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID>`
