---
description: "Fixes an existing defect with minimal repository rereading and compact bug work artifacts."
---

# /fix-bugs

## Purpose
Fix an existing defect in the smallest possible code slice. This stage must support both:
- standalone bug fixing in a repository that already has implemented code
- bug fixing attached to a currently active story work folder

Uses the `bug-fixer` agent.

## Required Input
Ask the user for these if they are not already provided:
- Bug ID
- Bug description
- Relationship mode: `standalone`, `current_story`, or `existing_work_id`
- At least one scoping hint:
  - suspected file
  - module / feature / service name
  - endpoint / screen
  - failing test name
  - stack trace location
  - reproduction steps

Ask for `existing_work_id` when relationship mode is `existing_work_id`.

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately.

STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide the missing bug ID,
bug description, relationship mode, `existing_work_id` when required, or any scoping hint after
the stage asks for them.

## Prerequisites
- `.sdlc/context/context-manifest.json` should exist. If it is missing, STOP and recommend
  `/setup-repo-context` first.

## Steps
1. Read `.sdlc/config/effort-dial.md` and `.sdlc/framework/context-rules.md`.
2. Resolve the work folder to use:
   - if relationship mode is `current_story`, read the current in-progress story work folder
     first
   - if relationship mode is `existing_work_id`, read that explicit work folder first
   - if relationship mode is `standalone`, use `.sdlc/work/<BUG-ID>/`
3. Read any existing `session.md`, `implementation-cache.json`, `implementation-plan.md`,
   `impact-map.md`, and `story-context.md` for that work folder before rereading repository
   context.
4. Perform a deterministic bug pre-pass before deeper reasoning:
   - identify the most concrete anchor from the bug input
  - identify the smallest exact source-file set needed for the fix
  - identify the smallest exact test-file set or regression gap for the bug
   - identify the narrowest reproduction and validation commands
  - identify applicable standards from `.sdlc/context/standards-summary.md`
  - record any missing facts in the compact cache instead of broad-reading immediately
5. Create or overwrite exactly once, never append, these files under `.sdlc/work/<ID>/` when a
   standalone bug work folder does not already exist:
   - `session.md`
   - `story-context.md`
   - `implementation-plan.md`
   - `impact-map.md`
   - `implementation-cache.json`
6. Create `requirement-analysis.md` only when the bug is ambiguous, risky, or blocked.
7. Invoke `bug-fixer` with only the compact work cache, the relevant instructions, and the
   exact files identified by the cache or the bug anchor.
8. Reproduce the bug with the cheapest focused check when feasible. If no reproduction exists,
   record `NOT_AVAILABLE` and validate with the cheapest discriminating command instead.
9. Run the narrowest validation commands recorded in `implementation-cache.json`.
10. Update these files under `.sdlc/work/<ID>/`:
    - `changes.md`
    - `session.md`
11. Update `implementation-cache.json` and `impact-map.md` only if the actual touched files,
    tests, anchors, or validation commands changed during bug fixing.

## Cost-Control Rules
- Do not broad-scan the repository when the bug input already provides a file, failing test,
  stack trace location, endpoint, or module anchor.
- Reuse an existing `implementation-cache.json` first. Only read markdown work artifacts or
	shared repository context for explicit missing facts.
- Read the smallest local slice first.
- Keep exact file inventories compact unless the defect genuinely crosses more boundaries.
- If the bug description is too vague to identify one concrete anchor, ask for one scoping
  hint instead of exploring broadly.

## Stop Condition
STOP after the bug fix and focused validation.

If required inputs remain missing after asking the user for them, STOP with:

```
CURRENT STAGE: Bug Fix
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing required bug-fix inputs>
NEXT RECOMMENDED COMMAND: None within the pipeline
```

```
CURRENT STAGE: Bug Fix
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <bug fixed from compact work cache with changed files recorded>
NEXT RECOMMENDED COMMAND: /implement-story only when the same story still has unfinished planned work; otherwise None within the pipeline
```