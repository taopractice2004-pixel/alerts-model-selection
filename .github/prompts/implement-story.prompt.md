---
description: "Implements a story from the compact story cache with minimal rereading."
---

# /implement-story

## Purpose
Implement the approved story using the smallest required context. Uses the `developer`
agent.

## Required Input
Jira ID.

If the only in-progress story is not inferable, ask for the Jira ID.

## Prerequisites
The story cache must exist:
- `.sdlc/work/<JIRA-ID>/session.md`
- `.sdlc/work/<JIRA-ID>/story-context.md`
- `.sdlc/work/<JIRA-ID>/implementation-plan.md`
- `.sdlc/work/<JIRA-ID>/impact-map.md`
- `.sdlc/work/<JIRA-ID>/implementation-cache.json`

If these files are missing, STOP and recommend `/analyze-story`.

## Steps
1. Read, in this order:
   - `implementation-cache.json`
   - `session.md`
   - exact source files and tests listed in `implementation-cache.json`
2. Read `implementation-plan.md` only if implementation intent or validation rationale is
   missing from `implementation-cache.json`.
3. Read `story-context.md` only if business constraints, work relationship, or tracker
   context is missing from `implementation-cache.json`.
4. Read `impact-map.md` only if adjacent dependencies or out-of-scope boundaries are missing
   from `implementation-cache.json`.
5. Read `requirement-analysis.md` only if the story case is `AMBIGUOUS`, unresolved
   questions remain, or the cache is incomplete.
6. Read `.sdlc/context/repo-profile.md` or `.sdlc/context/standards-summary.md` only if
   `repo_context_fallback_needed` is true or the cache explicitly records a missing fact.
7. If an exact file is missing from the cache but the slice is otherwise clear, patch only the
   missing cache field from the smallest nearby read, then continue.
8. Do not broad-scan the repository when the cache already identifies exact files or anchors.
9. Invoke `developer` with only the minimal story cache, the relevant instructions, and
   the exact touched files.
10. Run the narrowest validation commands recorded in `implementation-cache.json`. Do not run
    a full repository build or test suite when a focused check exists for the slice.
11. Update these files under `.sdlc/work/<JIRA-ID>/`:
   - `changes.md`
   - `session.md`
12. Update `implementation-cache.json` and `impact-map.md` only if the actual touched files,
   tests, or validation commands changed during implementation.

## Stop Condition
STOP after implementation and validation.

```
CURRENT STAGE: Story Implementation
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <code implemented from compact story cache>
NEXT RECOMMENDED COMMAND: None within the pipeline
```
