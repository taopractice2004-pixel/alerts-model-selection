---
description: "Starts a story in the cost-optimized pipeline and creates a compact story cache."
---

# /analyze-story

## Purpose
Begin story work with compact story artifacts and a machine-readable implementation cache.
Uses the `requirement-analyst` agent.

`implementation-cache.json` is the authoritative machine-readable output for later stages.
The markdown artifacts are thin human review documents and must not repeat inventories already
owned by the cache.

## Required Input
Ask the user for these if they are not already provided:
- Jira ID
- Jira Space / Project

If either is missing, STOP with `BLOCKED_MISSING_INFORMATION`.

## Steps
1. Read `.sdlc/config/effort-dial.md` and any existing `.sdlc/work/<JIRA-ID>/session.md`.
2. Follow `.sdlc/framework/context-rules.md`.
3. Perform a deterministic pre-pass before deeper reasoning:
   - identify likely touched layers
   - identify the smallest concrete scope anchors available from the story
   - identify likely exact source files, capped to the smallest useful set
   - identify likely exact tests, capped to the smallest useful set
   - identify build/test commands
   - identify applicable standards from `.sdlc/context/standards-summary.md`
4. Classify the story into one of three cases:
   - `SIMPLE`
   - `AMBIGUOUS`
   - `STALE_REPLAN`
5. Invoke `requirement-analyst` with only the compact repository cache, Jira inputs, and
   deterministic pre-pass output.
6. Create or overwrite exactly once, never append, these files under `.sdlc/work/<JIRA-ID>/`:
   - `session.md`
   - `story-context.md`
   - `implementation-plan.md`
   - `impact-map.md`
   - `implementation-cache.json`
7. Create `requirement-analysis.md` only when the case is `AMBIGUOUS`, or when unresolved
   questions remain after the pre-pass.
8. Keep each file compact and non-duplicated. Do not repeat standards, architecture, or
   impacted file lists across multiple files when one file already owns that fact.
9. Write `implementation-cache.json` as the authoritative source of truth for:
   - exact source files
   - exact test files
   - scope anchors
   - selected standards references
   - validation, coverage, and reproduction commands
   - unresolved questions and missing facts
10. Keep compact limits unless the story truly requires broader scope:
    - exact source files: at most 5
    - exact test files: at most 3
    - selected standards: at most 5
11. For `SIMPLE` stories, keep `story-context.md`, `implementation-plan.md`, and
    `impact-map.md` to the smallest useful human-review summary. Do not copy long acceptance
    criteria blocks or duplicate the exact file inventory from the cache.

## Stop Condition
STOP after the compact story cache is written.

```
CURRENT STAGE: Story Analysis
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <compact story cache created>
NEXT RECOMMENDED COMMAND: /implement-story
```
