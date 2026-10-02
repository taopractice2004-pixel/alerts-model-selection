---
name: production-implementation
description: "Executes only the Implementation Plan of an approved plan.md in production code: approval and staleness checks, reuse-first changes that match the plan's Contracts and Behavior Rules, deviation handling, scope check, and recording changed files. No build, tests, or validation. Use for /implement-story."
user-invocable: false
---

# Production Implementation

1. **Approval.** Read `## Approval Status` in `plan.md`.
   - `APPROVED`: set status `ANALYSIS_APPROVED` if it is still `ANALYSIS_DRAFT`.
   - `DRAFT` and the message explicitly approves ("approved", or the "Approve plan and implement"
     handoff): set `APPROVED`, fill Approved by (developer) and Date, set status `ANALYSIS_APPROVED`.
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
