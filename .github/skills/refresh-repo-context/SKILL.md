---
name: refresh-repo-context
description: "Primary SDLC stage. Use when repository structure, standards, major docs, or build/test setup changed after the repository cache was built. Reads the context manifest and refreshes only stale or changed cache sections instead of rebuilding everything. No dedicated agent and no code is implemented."
argument-hint: "(no arguments; refreshes stale sections of the current repository cache)"
user-invocable: true
disable-model-invocation: true
---

# Repository Context Refresh

Authoritative procedure for the `/refresh-repo-context` SDLC stage. This is the primary
execution path; the `prompt-refresh-repo-context` prompt only delegates here.

## Purpose
Use when repository structure, standards, major docs, or build/test setup changed after the
repository cache was built.

## Applicable Agent
None. Repository-level tooling only; no dedicated agent and no implementation.

## Shared Rules
Follow `.sdlc/framework/context-rules.md`, `.sdlc/framework/stage-rules.md`, and the global
rules in `.github/copilot-instructions.md`. Do not duplicate those rules here.

## Procedure
1. Read `.sdlc/context/context-manifest.json`. If it is missing, STOP and tell the user to run
   `/setup-repo-context` first.
2. Refresh only sections marked stale, missing, or explicitly changed by the user.
3. If `standards/` changed, re-read only the changed standards files and update
   `.sdlc/context/standards-summary.md` using
   `.sdlc/templates/standards-summary.template.md`, and update the affected rows in
   `.sdlc/context/standards-index.json` (and the matching compact
   `.github/instructions/standards/*.instructions.md` file when its enforceable rules changed).
4. Recalculate `.sdlc/context/context-exclusions.json` and the `## Repository Context
   Exclusions` section in `.github/copilot-instructions.md` only when repository structure,
   detected technologies, `.gitignore`, build configuration, or generated-output patterns
   materially changed. Otherwise leave both untouched. Update the exclusions section in place;
   do not duplicate it.
5. Update only the affected repository cache files:
   - `repo-profile.md`
   - `repository-map.md`
   - `project-docs-index.md`
   - `standards-summary.md`
   - `standards-index.json`
   - `context-exclusions.json`
   - `context-manifest.json`
6. Leave unaffected sections untouched.

## Cost-Control Behavior
- Do not rebuild the full cache. Touch only stale or changed sections.
- Re-read only standards files that actually changed.
- Recalculate `context-exclusions.json` only when exclusion-relevant facts materially changed;
  do not regenerate it unnecessarily.

## Outputs
Only the affected files among:
- `.sdlc/context/repo-profile.md`
- `.sdlc/context/repository-map.md`
- `.sdlc/context/project-docs-index.md`
- `.sdlc/context/standards-summary.md`
- `.sdlc/context/standards-index.json`
- `.sdlc/context/context-exclusions.json`
- `.sdlc/context/context-manifest.json`
- `.github/copilot-instructions.md` (only if the exclusions section materially changed)

## Stop Condition
STOP after the stale repository cache sections are refreshed. Do not chain into another stage.

```
CURRENT STAGE: Repository Context Refresh
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <stale repository cache sections refreshed>
NEXT RECOMMENDED COMMAND: /analyze-story
```
