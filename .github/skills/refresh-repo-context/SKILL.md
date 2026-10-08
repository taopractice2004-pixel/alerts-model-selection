---
name: refresh-repo-context
description: "Primary SDLC stage. Use when repository structure, standards, major docs, or build/test setup changed after the repository cache was built. Reads the context manifest and refreshes only stale or changed cache sections instead of rebuilding everything. No dedicated agent and no code is implemented."
argument-hint: "(no arguments; refreshes stale sections of the current repository cache)"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Repository Context Refresh

Complete authoritative procedure for the `/refresh-repo-context` SDLC stage. Developers invoke
this skill directly; there is no prompt wrapper or custom agent.

## Purpose
Use when repository structure, standards, major docs, or build/test setup changed after the
repository cache was built.

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context. Current cache state comes only from
  `.sdlc/context/manifest.json` and the other `.sdlc/context` artifacts.
- Write every required `.sdlc/context` artifact before returning; nothing else survives the
  fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.
- Repository-level tooling only; no application code is implemented.
- Permitted capabilities: `read`, `search`, and `edit` limited to `.sdlc/context/*`
  (and the compact `.github/instructions/standards/*.instructions.md` files).
  No command execution: never run shell commands, and never delete files. Write each cache file
  with the edit tool, overwriting it in place, so the cache is never left missing if the run
  stops partway.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Context Rules, Stage
Outputs, Repository Modes, exclusions). Do not duplicate those rules here.

## Procedure
1. Read `.sdlc/context/manifest.json`. If it is missing, STOP and tell the user to run
   `/setup-repo-context` first.
2. Refresh only sections marked stale, missing, or explicitly changed by the user.
   - If `repositoryMode` is `NEW_PROJECT` and real source code now exists, re-derive the
     affected `project-profile.md` sections (architecture, repository map, build/run/test,
     testing) from the code instead of the plan, keep the Requirements Summary, and switch
     `repositoryMode` to `EXISTING_PROJECT`.
   - If `repositoryMode` is `NEW_PROJECT` and the requirements document changed, re-read only
     its changed sections and update the Requirements Summary and planned structure.
3. If `standards/` changed, re-read only the changed standards files and update the affected
   entries in `.sdlc/context/manifest.json` → `standards.items` (and the matching compact
   `.github/instructions/standards/*.instructions.md` file when its enforceable rules changed).
4. Recalculate `.sdlc/context/manifest.json` → `exclusions` only when repository structure,
   detected technologies, `.gitignore`, build configuration, or generated-output patterns
   materially changed. Otherwise leave it untouched.
5. Update only the affected repository cache files:
   - `manifest.json` (mode, detected technologies, `exclusions`, `standards` routing index)
   - `project-profile.md` (overview, architecture, repository map, build/run/test, docs index,
     known gaps)
6. Leave unaffected sections untouched.

## Cost-Control Behavior
- Do not rebuild the full cache. Touch only stale or changed sections.
- Re-read only standards files that actually changed.

## Outputs
Only the affected files among:
- `.sdlc/context/manifest.json`
- `.sdlc/context/project-profile.md`
- `.github/instructions/standards/*.instructions.md` (only when a standard's enforceable rules changed)

## Stop Condition
STOP after the stale repository cache sections are refreshed. Do not chain into another stage.

```
CURRENT STAGE: Repository Context Refresh
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <stale repository cache sections refreshed>
NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID> — re-plan only stories whose work cache is stale; otherwise continue the story's next pending command from its log.md
```
