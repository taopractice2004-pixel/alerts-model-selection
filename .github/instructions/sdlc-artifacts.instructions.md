---
applyTo: ".sdlc/**"
description: "Rules for writing SDLC pipeline artifacts. Loaded only when working on files under .sdlc/."
---

# SDLC Artifact Instructions

- Artifacts are compact summaries, never copies. Do not paste full story texts, documents, or source files.
- Each fact has one owner file. Reference it instead of repeating it:
  - `plan.md` (stories only): the complete story plan - ACs, scope, impacted files, reuse, Implementation Plan, Validation Plan, Unit Test Plan, AC-to-Validation Mapping, risks, open questions, approval status. Each stage executes only its own sections.
  - `work.json`: lifecycle status, waiting-for-human reason, files actually changed, commands with working directories, standards, defects and their outcomes, standalone anchors and behaviors, missing facts
  - `log.md`: one appended entry per stage run, in the format of that stage's block in the log template
- `plan.md` and `work.json` are overwritten or patched. `log.md` is append-only: never rewrite earlier entries.
- When a stage learns that a recorded fact is wrong (a command, a path, a standard), fix it in its owner file immediately.
- Use explicit markers (`NOT_AVAILABLE`, `NOT_CONFIGURED`, `TO_BE_DISCOVERED`, `TO_BE_CONFIGURED`, `NOT_RUN`) instead of blanks or guesses.
- Repository context, one home per fact: `.sdlc/context/project-profile.md` (repository map and facts), `standards-summary.md` (condensed coding rules; instruction files point to it), `manifest.json` (verified commands, `contextPolicy`, build commit, section sources). It is a map, not a copy, and never holds secrets. Sections 10-13 of `project-profile.md` are developer-maintained and never overwritten by refresh.
- Use the work item ID consistently as the folder name: `.sdlc/work/<ID>/`.
- Write plain ASCII punctuation (`-` rather than long dashes) to avoid encoding problems.
