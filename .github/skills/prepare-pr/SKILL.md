---
name: prepare-pr
description: "Post-testing SDLC stage. Prepare everything a developer needs to manually create the pull request after /unit-testing passed with all acceptance criteria MET and no open bugs. Reads the minimum cached state plus a safe read-only diff, writes a concise PR draft to pr.md, records PR-preparation state in work.json, and runs as an isolated forked skill. Never creates a branch, commits, pushes, or opens the PR — that is a human action."
argument-hint: "<WORK-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Prepare PR

Complete authoritative procedure for the `/prepare-pr` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Prepare everything the developer needs to **manually** create the pull request once the work is
verified. It assembles a concise PR draft (`pr.md`) from already-recorded state and a safe
read-only diff. It never touches Git history and never opens the PR.

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
Release scribe: turn the verified work cache into a clear, review-ready PR draft.
- Documentation and state only. Do **not** modify production source or test code.
- **Git is read-only.** Do not `git add`, commit, push, create or switch a branch, open/approve
  a PR, or change Git configuration. A read-only diff command (for example `git diff --stat`,
  `git diff --name-only`, or `git status --porcelain`) is allowed only to summarize changed
  files; never a mutating Git command.
- Permitted capabilities: `read`, `search`, `edit` (`.sdlc` artifacts only), and `execute`
  limited to read-only Git/diff inspection commands.
- Do not repeat full repository analysis, rereading external trackers, BRDs, or broad
  documentation when the cache is sufficient.

## Required Input
Work ID. If the only in-progress work item is not inferable, ask for the Work ID.

## Prerequisites
The work cache must exist and be verified:
- `.sdlc/work/<ID>/work.json`, `plan.md`, and `log.md` exist. If missing, STOP and recommend
  `/analyze-story`.
- `work.json` → `test_fix_loop.status` must be `TESTS_PASSED` with `open_bugs: []`. If unit
  testing has not passed or bugs remain, STOP with `BLOCKED_MISSING_INFORMATION` and recommend
  `/unit-testing <ID> current_story`.
- If `pr.prepared` is already `true` (the PR draft exists and the work is already in the
  PR/review phase), do not re-prepare a new PR. STOP with `WAITING_FOR_HUMAN` and recommend
  `/l0-review <ID>`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Repository Modes, exclusions) and the applicable
`.github/instructions/*.instructions.md`. Do not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Read the minimum required state:
   - `work.json` (scope, acceptance criteria, exact files, build/test commands, `test_fix_loop`)
   - the latest relevant `log.md` entries (implementation + unit-testing results already
     recorded)
   - `plan.md` only for intent, strategy, boundaries, or risks not already in `work.json`
3. Summarize the actual changed files with a read-only diff command (never a mutating one). If
   no Git tooling is available, fall back to `work.json` → `exact_source_files` /
   `exact_test_files` and mark the diff source as `NOT_AVAILABLE`.
4. Read `.sdlc/context/*` only for a specific missing fact (for example a configuration or
   migration note) that the cache references but does not contain.
5. Write `.sdlc/work/<ID>/pr.md` (overwrite fully) with a concise draft containing:
   - **PR title** — short, imperative, prefixed with the work ID
   - **PR description** — one or two sentences of intent
   - **Story / requirement summary** — from `work.json` → `summary` / `acceptance_criteria`
   - **Implementation summary** — what changed, from `log.md` + `plan.md`
   - **Changed-files summary** — from the read-only diff (or the cached file lists)
   - **Acceptance Criteria → implementation / validation traceability** — each AC id → where it
     is implemented and the test that proves it (from `work.json` + unit-testing log)
   - **Test / build results already recorded** — do not re-run tests or builds
   - **Configuration / database / migration impacts** — only when relevant, else `None`
   - **Known risks / limitations** — from `plan.md`, else `None`
6. Update `work.json` → `pr`: `prepared: true`, `status: PREPARED`, `human_created: false`,
   leave `url` empty for the developer to fill after creating the PR. Do not modify
   `test_fix_loop` or `review`.
7. Update `.sdlc/work/<ID>/log.md`: set the Current Stage to `PREPARE_PR` and append an entry
   noting that `pr.md` was written, the changed-files source, and that human PR creation is
   required next. Do not alter the Test → Fix Loop counter.

## Cost-Control Behavior
- Reuse `work.json` and the existing `log.md` first; do not re-derive the story or rescan the
  repository.
- Use a single read-only diff command to list changed files; never re-run builds or tests —
  reuse the results already recorded by `/implement-story` and `/unit-testing`.
- Read a full `standards/*.md` or `.sdlc/context/*` file only for an exact missing fact.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`).

## Outputs
- `.sdlc/work/<ID>/pr.md`
- `work.json` → `pr`
- `.sdlc/work/<ID>/log.md`

## Stop Condition
STOP after the PR draft is written. Git actions and PR creation are human-only; do not perform
them and do not invoke another skill.

PR draft prepared:
```
CURRENT STAGE: Prepare PR
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: .sdlc/work/<ID>/pr.md, work.json, log.md
SUMMARY: PR draft written to pr.md (<n> changed files; all ACs MET; build/tests already recorded)
HUMAN ACTION: Review pr.md, then manually create the PR (or update the existing PR after a re-analysis cycle); AI does not touch Git
NEXT RECOMMENDED COMMAND: /l0-review <ID> — after the developer confirms the PR exists
```

PR already prepared (re-invoked):
```
CURRENT STAGE: Prepare PR
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: None
SUMMARY: PR draft already exists in pr.md; work is in the PR/review phase
HUMAN ACTION: Ensure the PR is created/updated
NEXT RECOMMENDED COMMAND: /l0-review <ID>
```

Not verified yet / missing cache:
```
CURRENT STAGE: Prepare PR
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <unit testing not passed / open bugs remain / work cache missing>
NEXT RECOMMENDED COMMAND: /unit-testing <ID> current_story — until tests pass with no open bugs | /analyze-story <ID> — if the work cache is missing
```
