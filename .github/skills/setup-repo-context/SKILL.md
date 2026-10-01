---
name: setup-repo-context
description: "Primary SDLC stage. Run once to build the compact repository cache for the cost-optimized pipeline, or later when a full rebuild of that cache is intentionally required. Inspects the repository and standards once, detects technologies and architecture, determines repository mode, and writes the `.sdlc/context` cache. No code is implemented."
argument-hint: "(no arguments; inspects the current repository)"
user-invocable: true
disable-model-invocation: true
---

# Repository Context Setup

Authoritative procedure for the `/setup-repo-context` SDLC stage. This is the primary
execution path; the `prompt-setup-repo-context` prompt only delegates here.

## Purpose
Run once when the pipeline is first added to a repository, or later when a full rebuild of the
repository cache is intentionally required.

## Applicable Agent
None. Repository-level tooling only; no dedicated agent and no implementation.

## Shared Rules
Follow `.sdlc/framework/context-rules.md`, `.sdlc/framework/stage-rules.md`, and the global
rules in `.github/copilot-instructions.md`. Do not duplicate those rules here.

## Procedure
1. Inspect the repository root once for technologies, frameworks, major folder structure,
   application architecture, build/test commands, documentation, `.github/`, and `standards/`.
2. If `standards/` exists, inspect each standards file and summarize it into a compact,
   deterministic catalog using `.sdlc/templates/standards-summary.template.md`. Confirm or
   update `.sdlc/context/standards-index.json` so each present standard maps to its full source
   file, its compact `.github/instructions/standards/*.instructions.md` file, and its applicable
   file types. Record ids and routing only; do not copy rule text into the index.
3. Determine repository mode:
   - `MODE_A_NEW_PROJECT`
   - `MODE_B_NEW_PROJECT_WITH_DOCS`
   - `MODE_C_EXISTING_PROJECT`
4. Identify repository-specific context exclusions and write
   `.sdlc/context/context-exclusions.json`:
   - inspect the existing `.gitignore` and the actual repository structure
   - using the detected technologies, identify safe noisy/generated/build/cache/binary/log
     paths that actually exist or are clearly applicable (for example `node_modules/`, `bin/`,
     `obj/`, `target/`, `dist/`, `build/`, `out/`, coverage output, generated-source folders,
     cache folders, temp files, logs, compiled artifacts)
   - use `.gitignore` as an important input, but do not blindly copy every entry
   - never exclude normal source code
   - never exclude configuration files by extension alone (`*.json`, `*.xml`, `*.yaml`,
     `*.yml`, `*.properties`) because they may carry build, runtime, API, architecture,
     deployment, or dependency information
   - keep the file compact; record only `excluded_paths`, `excluded_patterns`,
     `detected_technologies`, `generated_at`, and the standard `reason`
5. Add or update the `## Repository Context Exclusions` section in
   `.github/copilot-instructions.md`. If the section already exists, update it in place; do not
   duplicate it on re-runs.
6. Create or overwrite exactly once, never append, the repository cache files under
   `.sdlc/context/`:
   - `repo-profile.md`
   - `repository-map.md`
   - `project-docs-index.md`
   - `standards-summary.md`
   - `standards-index.json`
   - `context-exclusions.json`
   - `context-manifest.json`
7. Keep all repository cache files compact. Do not copy full standards documents or full
   repository listings into the cache.
8. If information is unavailable, write `NOT_AVAILABLE` or `TO_BE_DISCOVERED` instead of
   guessing.

## Cost-Control Behavior
- Inspect the repository only as far as the cache requires; do not broaden the scan.
- Summarize standards; never copy full documents.
- Write each cache file exactly once.

## Outputs
- `.sdlc/context/repo-profile.md`
- `.sdlc/context/repository-map.md`
- `.sdlc/context/project-docs-index.md`
- `.sdlc/context/standards-summary.md`
- `.sdlc/context/standards-index.json`
- `.sdlc/context/context-exclusions.json`
- `.sdlc/context/context-manifest.json`
- `.github/copilot-instructions.md` (`## Repository Context Exclusions` section added or updated)

## Stop Condition
STOP after the repository cache is written. Do not chain into another stage.

```
CURRENT STAGE: Repository Context Setup
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>
SUMMARY: <compact repository cache created>
NEXT RECOMMENDED COMMAND: /analyze-story
```
