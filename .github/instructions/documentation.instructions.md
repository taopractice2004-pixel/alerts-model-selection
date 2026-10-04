---
applyTo: "**"
description: "Rules for keeping SDLC context artifacts concise, current, and non-duplicated."
---

# Documentation Instructions

These rules apply to every SDLC skill when writing to `.sdlc/context/*`, `.sdlc/work/<ID>/*`, or
any other framework artifact.

## Core Rules

1. **Be concise and reusable.** Context files (`project-profile.md`, `plan.md`, etc.)
   are summaries and indexes, not copies. Never paste full Jira tickets, full BRDs, or full
   source files into a context artifact.
2. **No unnecessary duplication.** If information already exists in another context file,
   reference it rather than repeating it (e.g. `plan.md` should reference `work.json` for
   files and commands, and `project-profile.md` rather than re-describing the architecture).
3. **Keep cached information current.** When a stage discovers information that changes a
   previously recorded fact (e.g. a build command, a standard, an architecture note), update
   the source-of-truth file immediately rather than leaving stale duplicates.
4. **Maintain traceability.** Every artifact should make it possible to trace
   repository context → requirement analysis → implementation without needing chat history.
   Use the active story ID or bug ID consistently as the folder/file key
   (`.sdlc/work/<ID>/...`).
5. **Use explicit status markers** (`NOT_AVAILABLE`, `NOT_CONFIGURED`, `TO_BE_DISCOVERED`,
   `TO_BE_CONFIGURED`) instead of leaving fields blank or fabricating plausible-sounding
   content.
6. **One fact, one owner.** Keep exactly the file set defined under Pipeline Files in
   `.github/copilot-instructions.md` and `.sdlc/templates/`; do not create extra work files.
