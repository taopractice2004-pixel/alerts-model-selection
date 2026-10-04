---
name: setup-repo-context
description: "Primary SDLC stage. Run once to build the compact repository cache for the cost-optimized pipeline, or later when a full rebuild of that cache is intentionally required. Takes the repository type: 1 = existing repository (inspects the code), 2 = new repository (extracts context from a user-provided requirements document). Writes the `.sdlc/context` cache. No code is implemented."
argument-hint: "<1 = existing repo | 2 = new repo> [requirements doc path, required for 2]"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Repository Context Setup

Complete authoritative procedure for the `/setup-repo-context` SDLC stage. Developers invoke
this skill directly; there is no prompt wrapper or custom agent.

## Purpose
Run once when the pipeline is first added to a repository, or later when a full rebuild of the
repository cache is intentionally required. Builds the cache from the existing code (option 1)
or from a requirements document for a repository that has no code yet (option 2).

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context; derive everything from the repository itself and,
  for option 2, the provided requirements document.
- Write every required `.sdlc/context` artifact before returning; nothing else survives the
  fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.
- Repository-level tooling only; no application code is implemented.

## Required Input
- Repository type, given beside the command:
  - `1` — existing repository → `repositoryMode: EXISTING_PROJECT`
  - `2` — new repository → `repositoryMode: NEW_PROJECT`
- For `2` only: the requirements document (a path in the repository, or an attached file).

Examples: `/setup-repo-context 1`, `/setup-repo-context 2 docs/requirements.md`.

If the repository type is missing, ask: "Is this an existing repository (1) or a new
repository (2)?" If it is `2` and no requirements document is given, ask for it. STOP with
`BLOCKED_MISSING_INFORMATION` only when the user does not provide them after being asked, or
when the document cannot be read.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Context Rules, Stage
Outputs, Repository Modes, exclusions). Do not duplicate those rules here.

## Procedure
1. Resolve the repository type from the input (ask if missing, as above).
2. Gather context:
   - **Option 1 (existing):** inspect the repository root once for technologies, frameworks,
     major folder structure, application architecture, build/test commands, documentation,
     `.github/`, and `standards/`.
   - **Option 2 (new):** read the requirements document once and extract only what the cache
     needs: product purpose, planned stack/technologies, planned architecture and layering,
     planned modules/services and folder layout, external interfaces and integrations, data
     store, non-functional requirements and constraints (security, performance, compliance),
     and any stated build/test tooling. Also glance at the repository root for anything
     already present (`.gitignore`, `standards/`, `.github/`, scaffolding). Never invent facts
     the document does not state; record them as `TO_BE_DECIDED` (a decision the first
     implementation must make and record in `plan.md`) or `NOT_AVAILABLE`.
3. If `standards/` exists, inspect each standards file and summarize it into a compact,
   deterministic catalog in `.sdlc/context/standards-summary.md`, keeping its table shape.
   Confirm or update `.sdlc/context/manifest.json` → `standards.items` so each present standard
   maps to its full source file, its compact `.github/instructions/standards/*.instructions.md`
   file, and its applicable file types. Record ids and routing only; do not copy rule text into
   the index. For option 2, mark standards that match the planned stack as applicable and the
   rest as `NOT_APPLICABLE`.
4. Set `manifest.json` → `repositoryMode` to `EXISTING_PROJECT` (option 1) or `NEW_PROJECT`
   (option 2). For option 2, record the document path in `detected.sourceInputs.requirementsDocs`.
5. Identify repository-specific context exclusions and write them to
   `.sdlc/context/manifest.json` → `exclusions`:
   - inspect the existing `.gitignore` and the actual repository structure
   - using the detected (option 1) or planned (option 2) technologies, identify safe
     noisy/generated/build/cache/binary/log paths that actually exist or are clearly applicable
     (for example `node_modules/`, `bin/`, `obj/`, `target/`, `dist/`, `build/`, `out/`,
     coverage output, generated-source folders, cache folders, temp files, logs, compiled
     artifacts); for option 2 these are the standard paths the planned stack will produce
   - use `.gitignore` as an important input, but do not blindly copy every entry
   - never exclude normal source code
   - never exclude configuration files by extension alone (`*.json`, `*.xml`, `*.yaml`,
     `*.yml`, `*.properties`) because they may carry build, runtime, API, architecture,
     deployment, or dependency information
   - keep the section compact; record only `excludedPaths`, `excludedPatterns`, and the
     standard `reason`, with the technologies in `detected.technologies`
6. Add or update the `## Repository Context Exclusions` section in
   `.github/copilot-instructions.md`. If the section already exists, update it in place; do not
   duplicate it on re-runs.
7. Create or overwrite exactly once, never append, the repository cache files under
   `.sdlc/context/`:
   - `manifest.json` (mode, technologies, `exclusions`, `standards` routing index)
   - `project-profile.md` (overview, architecture, repository map, build/run/test, docs index,
     known gaps). For option 2, set its Source line to the requirements document, describe the
     **planned** architecture and folder layout from the document, list the document in Project
     Docs, and list the document's key requirements / non-functional constraints compactly in
     the Requirements Summary section — summarize, never copy the document body.
   - `standards-summary.md`
8. Keep all repository cache files compact. Do not copy full standards documents, full
   requirements documents, or full repository listings into the cache.
9. If information is unavailable, write `NOT_AVAILABLE`, `TO_BE_DISCOVERED` (option 1), or
   `TO_BE_DECIDED` (option 2) instead of guessing.

## Cost-Control Behavior
- Option 1: inspect the repository only as far as the cache requires; do not broaden the scan.
- Option 2: read the requirements document once; later stages use the compact summary in
  `project-profile.md` instead of rereading the document.
- Summarize standards; never copy full documents.
- Write each cache file exactly once.

## Outputs
- `.sdlc/context/manifest.json`
- `.sdlc/context/project-profile.md`
- `.sdlc/context/standards-summary.md`
- `.github/copilot-instructions.md` (`## Repository Context Exclusions` section added or updated)

## Stop Condition
STOP after the repository cache is written. Do not chain into another stage.

```
CURRENT STAGE: Repository Context Setup
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <compact repository cache created from existing code | from requirements doc>
NEXT RECOMMENDED COMMAND: /analyze-story <STORY-ID> (with story description and acceptance criteria)
```

When blocked: `NEXT RECOMMENDED COMMAND: /setup-repo-context <1 | 2 <requirements doc>> — once the missing input is supplied`.
