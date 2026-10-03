---
name: implement-story
description: "Executes only the Implementation Plan of an approved plan.md in production code: approval and staleness checks, reuse-first changes that match the plan's Contracts and Behavior Rules, deviation handling, scope check, and recording changed files. No build, tests, or validation. Invoke directly as /implement-story <ID> [approved|continue]."
argument-hint: "Story ID (add 'approved' to approve the plan, 'continue' to proceed despite a stale plan)"
context: fork
user-invocable: true
disable-model-invocation: true
---

# /implement-story

This file is the complete, authoritative workflow for this stage. It runs in its own isolated
context (`context: fork`): use only this message and the files on disk, never earlier chat history.
Cross-stage state comes only from `.sdlc/` artifacts.

## Stage Contract
- Role: developer. Edit production code only, within an approved plan; no code changes before the
  plan is approved.
- Input: story ID; otherwise the only `.sdlc/work/` folder with status `ANALYSIS_DRAFT` or
  `ANALYSIS_APPROVED`; otherwise stop with `BLOCKED_MISSING_INFORMATION`. Optional: `approved`,
  `continue`.
- Requires: `.sdlc/work/<ID>/plan.md` and `work.json` (else recommend `/analyze-story <ID>`); status
  `ANALYSIS_DRAFT`, `ANALYSIS_APPROVED`, or `IMPLEMENTATION_IN_PROGRESS` (later: recommend
  `/unit-testing <ID>` or `/fix-bugs <ID>`).
- Start from: `work.json`, `plan.md`.
- Boundaries: never create, edit, or run tests; never build, lint, or run coverage; never validate
  acceptance criteria or set the story `COMPLETE`. Commands: read-only `git diff` only (staleness and
  scope checks). Stop before any major deviation from the plan. Do not invoke another skill or
  custom agent.
- Result: production changes recorded; status `IMPLEMENTATION_COMPLETE`.
- Return: the stage report defined in `.github/copilot-instructions.md`, then STOP.
  Next (manual): `/unit-testing <ID>`; for a plan or scope problem, `/analyze-story <ID>`.

## Procedure

1. **Approval.** Read `## Approval Status` in `plan.md`.
   - `APPROVED`: set status `ANALYSIS_APPROVED` if it is still `ANALYSIS_DRAFT`.
   - `DRAFT` and the message explicitly approves ("approved"): set `APPROVED`, fill Approved by (developer) and Date, set status `ANALYSIS_APPROVED`.
   - Otherwise stop with `WAITING_FOR_HUMAN` and say what to review or answer in `plan.md`.
2. **Staleness.** Skip when resuming `IMPLEMENTATION_IN_PROGRESS`, or when `analyzed_at_commit` is null
   or git is unavailable (log why). Run
   `git diff --name-only <analyzed_at_commit> HEAD -- <paths in Files to Modify and Files to Create>`.
   Listed paths and no "continue" in the message: stop with `WAITING_FOR_HUMAN`, list them, set
   `waiting_for_human`, recommend `/analyze-story <ID>` or `/implement-story <ID> continue`. With
   "continue": log the decision and go on.
3. Set status `IMPLEMENTATION_IN_PROGRESS`. Read Impacted Files, the Reuse / Existing Patterns example
   files, and the `standards-summary.md` sections in `selected_standards`; read `project-profile.md`
   only for `missing_facts`.
4. **Execute Steps** in order, including `[wiring]` steps: the smallest change at the named class or
   method, in the style of the example files, inside In Scope. Match `Contracts` exactly and implement
   every `Behavior Rules` line. After a re-analysis, skip steps the code already satisfies (log them).
   Files to Create (`TO_CREATE` or `PROPOSED`) are approved creation scope once the plan is approved:
   create them and their folders at the planned paths. When Reuse is `NOT_ESTABLISHED`, follow
   `standards-summary.md` and the plan instead of example files.
5. **Deviations.**
   - Minor (allowed; log it): a small same-layer addition a step needs (private method, file next to a
     planned file in the same layer, field used only by this story).
   - Major (stop before making it): a contract different from `Contracts`; shared contract or API
     changes; schema or migrations; a new package; anything in Do Not Modify, `doNotModify`, or the
     plan's stop points; a significant architectural change; major scope expansion. Set
     `waiting_for_human`, keep status `IMPLEMENTATION_IN_PROGRESS`, explain, stop.
   - A plan or requirement that cannot be implemented as written: stop the same way; recommend
     `/analyze-story <ID>`.
   - An unrelated existing defect: log it; do not fix it.
6. **Scope check** with `git diff --stat` (or the source-control changes list): every step done, every
   behavior rule implemented, contracts matched; only planned files or logged minor deviations
   changed; no debug code, leftover logging, or commented-out code; nothing duplicated.
7. **Record.** Append the `/implement-story` entry from `.sdlc/templates/log.template.md`. In
   `work.json` set `files_changed.production`, `waiting_for_human` = null, status
   `IMPLEMENTATION_COMPLETE`.
