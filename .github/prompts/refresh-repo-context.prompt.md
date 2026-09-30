---
description: "Refreshes only stale or changed repository cache sections for the cost-optimized pipeline."
---

# /refresh-repo-context

## Purpose
Use when repository structure, standards, major docs, or build/test setup changed after the
repository cache was built.

## Does NOT use a dedicated agent
Repository-level tooling only.

## Steps
1. Read `.sdlc/context/context-manifest.json`. If it is missing, STOP and tell the user to
   run `/setup-repo-context` first.
2. Refresh only sections marked stale, missing, or explicitly changed by the user.
3. If `standards/` changed, re-read only the changed standards files and update
   `.sdlc/context/standards-summary.md` using
   `.sdlc/templates/standards-summary.template.md`.
4. Update only the affected repository cache files:
   - `repo-profile.md`
   - `repository-map.md`
   - `project-docs-index.md`
   - `standards-summary.md`
   - `context-manifest.json`
5. Leave unaffected sections untouched.

## Stop Condition
STOP after the stale repository cache sections are refreshed.

```
CURRENT STAGE: Repository Context Refresh
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <stale repository cache sections refreshed>
NEXT RECOMMENDED COMMAND: /analyze-story
```
