---
description: "Builds a compact repository cache for the cost-optimized pipeline."
---

# /setup-repo-context

## Purpose
Run this once when the pipeline is first added to a repository, or later when a full
rebuild of the repository cache is intentionally required.

## Does NOT use a dedicated agent
Repository-level tooling only.

## Steps
1. Inspect the repository root for technologies, frameworks, major folder structure,
   application architecture, build/test commands, documentation, `.github/`, and
   `standards/`.
2. If `standards/` exists, inspect each standards file and summarize it into a compact,
   deterministic catalog using `.sdlc/templates/standards-summary.template.md`.
3. Determine repository mode:
   - `MODE_A_NEW_PROJECT`
   - `MODE_B_NEW_PROJECT_WITH_DOCS`
   - `MODE_C_EXISTING_PROJECT`
4. Create or overwrite exactly once, never append, the repository cache files under
   `.sdlc/context/`:
   - `repo-profile.md`
   - `repository-map.md`
   - `project-docs-index.md`
   - `standards-summary.md`
   - `context-manifest.json`
5. Keep all repository cache files compact. Do not copy full standards documents or full
   repository listings into the cache.
6. If information is unavailable, write `NOT_AVAILABLE` or `TO_BE_DISCOVERED` instead of
   guessing.

## Stop Condition
STOP after the repository cache is written.

```
CURRENT STAGE: Repository Context Setup
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <compact repository cache created>
NEXT RECOMMENDED COMMAND: /analyze-story
```
