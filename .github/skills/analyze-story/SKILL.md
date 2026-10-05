---
name: analyze-story
description: "Primary SDLC stage. Begin story work from user-provided story details by producing compact human-review artifacts (`plan.md`, `log.md`) and the authoritative machine-readable `work.json`. Runs a deterministic pre-pass, classifies the story (SIMPLE / AMBIGUOUS / STALE_REPLAN), identifies the smallest exact file set, and runs as an isolated forked skill. Use when starting or re-planning a story."
argument-hint: "<STORY-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Story Analysis

Complete authoritative procedure for the `/analyze-story` SDLC stage. Developers invoke this
skill directly; there is no prompt wrapper or custom agent.

## Purpose
Begin story work with compact story artifacts and a machine-readable work cache.
`work.json` is the authoritative machine-readable output for later stages. `plan.md` and
`log.md` are thin human-review documents and must not repeat inventories already owned by
`work.json`.

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
deterministic work scope; classify it `SIMPLE`, `AMBIGUOUS`, or `STALE_REPLAN`; select only the
relevant standard ids.
- Analysis and cache generation only. Do not implement, modify application source code, or run
  builds/tests/terminals.
- Permitted capabilities: `read`, `search`, and `edit` limited to `.sdlc/work/<STORY-ID>/`
  artifacts. No command execution.
- Do not reread external trackers, full standards documents, BRDs, or broad repository
  documentation when cached context is sufficient; rely on the compact
  `.github/instructions/**/*.instructions.md` standards.

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
1. Apply the Effort Dial and read any existing `.sdlc/work/<STORY-ID>/log.md`.
2. Follow the Read Order and Context Rules.
3. Perform a deterministic pre-pass before deeper reasoning:
   - identify likely touched layers
   - identify the smallest concrete scope anchors available from the story
   - identify likely exact source files, capped to the smallest useful set
   - identify likely exact tests, capped to the smallest useful set
   - identify the narrowest build/compile command for the tech stack (used by
     `/implement-story` and `/fix-bugs`) and the narrowest unit-test and coverage commands
     (used only by `/unit-testing`)
   - select applicable standard ids from `.sdlc/context/manifest.json` → `standards.items` based on the
     touched file types/layers (for example `coding`, `backend-dotnet`, `api-rest`,
     `database`). Record ids, not rule text; the compact rules auto-apply through
     `.github/instructions/standards/*.instructions.md`.
4. Classify the story into one of three cases:
   - `SIMPLE`
   - `AMBIGUOUS`
   - `STALE_REPLAN`
5. Perform the requirement analysis directly in this forked context, using only the compact
   repository cache, the user-provided story inputs, and the deterministic pre-pass output.
6. Create or overwrite exactly once, never append, these files under `.sdlc/work/<STORY-ID>/`
   from `.sdlc/templates/`:
   - `work.json`
   - `plan.md`
   - `log.md` (status header plus the first entry for this run)
7. Fill the Requirement Analysis section of `plan.md` only when the case is `AMBIGUOUS`, or
   when unresolved questions remain after the pre-pass; otherwise write "Not required".
8. Keep each file compact and non-duplicated. Do not repeat standards, architecture, or
   impacted file lists across multiple files when one file already owns that fact.
9. Write `work.json` as the authoritative source of truth for:
   - acceptance criteria, split into testable items with ids (`AC1`, `AC2`, …) — these are what
     `/unit-testing` verifies
   - exact source files
   - exact test files
   - scope anchors
   - selected standards references, recorded as standard ids from
     `.sdlc/context/manifest.json` → `standards.items` (for example
     `selected_standards: ["coding", "backend-dotnet", "api-rest"]`)
   - `build_commands`, `unit_test_commands`, and `coverage_commands`
   - `test_fix_loop` initialized to `max_fix_iterations: 3`, `fix_iteration: 0`,
     `status: NOT_STARTED`, `open_bugs: []`
   - unresolved questions and missing facts
10. Keep compact limits unless the story truly requires broader scope:
    - exact source files: at most 5
    - exact test files: at most 3
    - selected standards: at most 5
11. For `SIMPLE` stories, keep `plan.md` to the smallest useful human-review summary. Do not copy long acceptance
    criteria blocks or duplicate the exact file inventory from the cache.

## Cost-Control Behavior
- `work.json` is the single authoritative scope record; `plan.md` and `log.md` stay thin and
  must not duplicate it.
- Honor the file-count caps above unless the story genuinely crosses more boundaries.
- Fill the plan's Requirement Analysis section only for `AMBIGUOUS` cases or unresolved
  questions.
- Do not broad-scan the repository when the story already yields concrete anchors.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.

## Outputs
- `.sdlc/work/<STORY-ID>/work.json`
- `.sdlc/work/<STORY-ID>/plan.md`
- `.sdlc/work/<STORY-ID>/log.md`

## Stop Condition
STOP after the compact story cache is written. Do not invoke `/implement-story`; only
recommend it. A human reviews the artifacts and invokes the next skill.

```
CURRENT STAGE: Story Analysis
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <compact story cache created; case; number of acceptance criteria>
NEXT RECOMMENDED COMMAND: /implement-story <STORY-ID>
```

When blocked: `NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID> — once <missing input> is supplied`.
