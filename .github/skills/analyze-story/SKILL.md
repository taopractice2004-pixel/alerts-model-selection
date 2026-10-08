---
name: analyze-story
description: "Primary SDLC stage. Begin story work from user-provided story details by producing the authoritative machine-readable `work.json`, a complete readable `plan.md` for developer review (files to change, steps, AC coverage, contract changes, risks, open questions), and `log.md`. Runs a deterministic pre-pass, identifies the smallest exact file set, raises open questions instead of guessing, and runs as an isolated forked skill. Use when starting or re-planning a story."
argument-hint: "<STORY-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Story Analysis

Complete authoritative procedure for the `/analyze-story` SDLC stage. Developers invoke this
skill directly; there is no prompt wrapper or custom agent.

## Purpose
Begin story work with a machine-readable work cache and a complete, readable plan the
developer can review and approve.
`work.json` is the authoritative machine-readable output for later stages and the only file the
developer edits. `plan.md` is the complete, readable view of that plan for developer review: it
renders the facts owned by `work.json` (files to change, steps, AC coverage, contracts, risks,
questions) and never introduces facts that are not in `work.json`. This stage is the only one
that writes `plan.md` for a story; later stages do not update it. `log.md` is the stage log.

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context. Cross-stage state comes only from persistent
  `.sdlc/` artifacts (`.sdlc/context/*`, `.sdlc/work/<STORY-ID>/*`).
- Write every required `.sdlc` artifact before returning; nothing else survives the fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.

## Role
Requirement analyst: turn the user-provided story plus repository context into a compact,
deterministic work scope, and raise anything that needs a human decision as an open question.
- Analysis and cache generation only. Do not implement, modify application source code, or run
  builds/tests/terminals.
- Permitted capabilities: `read`, `search`, and `edit` limited to `.sdlc/work/<STORY-ID>/`
  artifacts. No command execution.
- Do not reread external trackers, full standards documents, BRDs, or broad repository
  documentation when cached context is sufficient.

## Required Input
The user provides all requirement information directly. Ask for these if they are not already
provided:
- Story ID
- Story Description
- Acceptance Criteria

Do not fetch the story from Jira, MCP, Confluence, or any other external source.

If any of these is missing, STOP with `BLOCKED_MISSING_INFORMATION`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Repository Modes, exclusions). Do not duplicate those rules here.

## Procedure
1. Apply the Effort Dial and read any existing `.sdlc/work/<STORY-ID>/log.md`. On a re-plan,
   if an existing `work.json` has answers in `unresolved_questions[].answer` or in
   `constraints` entries starting with `Q<n>:`, treat those answers as authoritative developer
   input and carry them into the new plan (do not ask the same question again; number any new
   question after the highest existing `Q<n>`).
2. Follow the Read Order and Context Rules.
3. Perform a deterministic pre-pass before deeper reasoning:
   - identify likely touched layers
   - identify likely exact source files, capped per the Context Rules, and for each one
     decide its `action` (`create` / `modify` / `delete`), its `anchor` (class, function,
     method, or component), the `change` it needs, the AC ids it implements, and the `reason`
     it was chosen
   - verify every path: a `modify` / `delete` file must exist; a `create` file's parent folder
     must exist (or be planned, for `NEW_PROJECT`) and its name must follow the repository's
     naming pattern. Never record a path you have not confirmed this way.
   - identify likely exact tests, capped per the Context Rules, with `action` and the AC ids
     each covers
   - draft the ordered `implementation_steps` (each step names the file it touches)
   - draft `planned_tests`: for every AC at least one `success` test, plus `failure` / `edge`
     tests where the criterion has error or boundary behavior
   - check for contract / data changes (API, database, config, dependencies, events) and
     record `None` for each one that does not apply
   - identify callers and consumers that are affected but not modified (`adjacent_dependencies`
     with `why`), assumptions the plan makes, and the risks (always at least one line)
   - identify the narrowest build/compile command for the tech stack (used by
     `/implement-story` and `/fix-bugs`) and the narrowest unit-test and coverage commands
     (used only by `/unit-testing`). Every test project in `unit_test_commands` must have a
     matching coverage command in `coverage_commands` (or one coverage command that covers all
     of them), so coverage of every changed project is measured
4. Perform the requirement analysis directly in this forked context, using only the compact
   repository cache, the user-provided story inputs, and the deterministic pre-pass output.
5. Raise open questions instead of guessing:
   - anything that needs human resolution becomes a question in `unresolved_questions`, never
     only a note
   - any AC that does not map to at least one source file and at least one planned test
     becomes an open question
6. Create or overwrite exactly once, never append, these files under `.sdlc/work/<STORY-ID>/`
   from `.sdlc/templates/`:
   - `work.json`
   - `plan.md`
   - `log.md` (status header plus the first entry for this run)
7. Write `work.json` as the authoritative source of truth for:
   - acceptance criteria, split into testable items with ids (`AC1`, `AC2`, …) — these are what
     `/unit-testing` verifies
   - `summary` (1–3 lines)
   - exact source files as objects (`path`, `action`, `anchor`, `change`, `ac`, `reason`)
   - exact test files as objects (`path`, `action`, `ac`)
   - `adjacent_dependencies` as objects (`path`, `why`)
   - `implementation_steps`, `planned_tests`, `contract_changes`, `boundaries`
     (`primary_slice`, `out_of_scope`), `assumptions`, and `risks`
   - `scope_override_reason` when a Context Rules cap is exceeded
   - `build_commands`, `unit_test_commands`, and `coverage_commands`
   - `test_fix_loop` initialized to `max_fix_iterations: 3`, `fix_iteration: 0`,
     `status: NOT_STARTED`, `open_bugs: []`
   - `code_review` initialized to `max_review_fix_iterations: 2`, `review_fix_iteration: 0`,
     `status: NOT_STARTED`, `analyzer_commands: []`, `findings: []`
   - unresolved questions and missing facts. Write each question for the developer as
     `{ "id": "Q1", "question": "...", "answer": "" }` in `unresolved_questions`, with the
     `answer` left empty for the developer to fill in. Keep previously answered questions once,
     as their `Q<n>: … → …` entries in `constraints`.
   - every text value as one short sentence (see Context Rules); explanations go in `log.md`
   - `plan_review` set to `status: PENDING`, `approved_via` and `date` empty — on every run,
     including re-plans. Never set `APPROVED`; only `/implement-story <STORY-ID> approved`
     does that.
   Do not copy standards text or architecture from `.sdlc/context/` into `work.json` or
   `plan.md`.
8. Render `plan.md` from `work.json` using every section of `.sdlc/templates/plan.template.md`
   (write `None` for a section that does not apply; never drop a section), sized per the
   Effort Dial. The developer must be able to review the whole plan from `plan.md` alone.
9. Choose the result status:
   - `WAITING_FOR_HUMAN` when `unresolved_questions` has any question with an empty
     `answer` — the developer must answer them in `work.json` before approving.
   - `STAGE_PASSED` otherwise — the plan is ready for developer review.
   In both cases set the `log.md` status header to Story Analysis = the result,
   Plan Review = `PENDING`, Current Stage = `PLAN_REVIEW`.

## Outputs
- `.sdlc/work/<STORY-ID>/work.json` (with `plan_review.status: PENDING` and any open
  questions with empty `answer` fields)
- `.sdlc/work/<STORY-ID>/plan.md` (complete readable plan: files to change, steps, AC
  coverage, contract / data changes, risks, open questions, review checklist)
- `.sdlc/work/<STORY-ID>/log.md`

## Stop Condition
STOP after `work.json`, `plan.md`, and `log.md` are written. Do not invoke `/implement-story`; only
recommend it. The developer reviews the artifacts, answers any open questions in `work.json`,
and approves the plan by invoking `/implement-story <STORY-ID> approved`.

No open questions:
```
CURRENT STAGE: Story Analysis
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <n> acceptance criteria; <n> files (<c> create, <m> modify, <d> delete); <n> planned tests; contract changes: <None | list>; Plan review: PENDING
NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID> approved — after reviewing plan.md and work.json
```

Open questions for the developer:
```
CURRENT STAGE: Story Analysis
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <list>
SUMMARY: <n> acceptance criteria; <n> files (<c> create, <m> modify, <d> delete); <n> planned tests; <n> open questions (<Q ids>) in work.json → unresolved_questions; Plan review: PENDING
NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID> approved — after answering every question in .sdlc/work/<STORY-ID>/work.json → unresolved_questions[].answer and saving the file
```

When blocked: `NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID> — once <missing input> is supplied`.
