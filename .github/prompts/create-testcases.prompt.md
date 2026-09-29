---
description: "Creates or updates unit tests, runs them, and verifies scoped coverage with minimal repository rereading."
---

# /create-testcases

## Purpose
Create or update unit tests for a requested code slice, then run the narrowest unit-test and
coverage commands for that slice. This stage must support both:
- continuation after story implementation or bug fixing
- standalone unit-test creation in a repository that already has implemented code

Uses the `test-author` agent.

## Required Input
Ask the user for these if they are not already provided:
- Work ID
- Relationship mode: `standalone`, `current_story`, or `existing_work_id`
- Target behavior to cover with unit tests
- At least one scope anchor:
  - source file path
  - class name
  - function or method name
  - module / feature / service name
  - existing changed file
  - existing test file

Ask for `existing_work_id` when relationship mode is `existing_work_id`.

## Optional Input
- Test file hint
- Coverage goal for the requested scope
- Narrow unit-test command when the user already knows it

If any required value is missing, ask the user for the missing fields first instead of stopping
immediately.

STOP with `BLOCKED_MISSING_INFORMATION` only when the user does not provide the missing work ID,
relationship mode, target behavior, `existing_work_id` when required, or any scope anchor after
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
   - if relationship mode is `standalone`, use `.sdlc/work/<WORK-ID>/`
3. Read any existing `session.md`, `implementation-cache.json`, `implementation-plan.md`,
   `impact-map.md`, `story-context.md`, and `changes.md` for that work folder before rereading
   repository context.
4. Perform a deterministic test pre-pass before deeper reasoning:
   - identify the most concrete source anchor from the input
   - identify the likely existing exact unit test file for that slice
   - if a relevant unit test file exists, plan to edit it
   - if no relevant unit test file exists, plan to create a new one using the repository's
     existing test naming and placement pattern
   - identify the narrowest unit-test command
   - identify the narrowest scoped coverage command when coverage tooling exists
   - identify applicable standards from `.sdlc/context/standards-summary.md`
   - record any missing facts in the compact cache instead of broad-reading immediately
5. Create or overwrite exactly once, never append, these files under `.sdlc/work/<ID>/` when a
   standalone testcase work folder does not already exist:
   - `session.md`
   - `story-context.md`
   - `implementation-plan.md`
   - `impact-map.md`
   - `implementation-cache.json`
6. Create `requirement-analysis.md` only when the testcase scope is ambiguous, risky, or
   blocked.
7. Invoke `test-author` with only the compact work cache, the relevant instructions, and the
   exact source and test files identified by the cache or the scope anchor.
8. Create or update unit tests only. Prefer editing an existing nearby unit test file; create a
   new unit test file only when no relevant one exists.
9. Run the narrowest unit-test command recorded in `implementation-cache.json`.
10. Run the narrowest scoped coverage command recorded in `implementation-cache.json` when
    coverage tooling exists. If coverage is not configured, record `NOT_CONFIGURED` instead of
    fabricating metrics.
11. Update these files under `.sdlc/work/<ID>/`:
    - `changes.md`
    - `session.md`
12. Update `implementation-cache.json` and `impact-map.md` only if the actual touched source
    files, test files, validation commands, or coverage commands changed during testcase
    creation.

## Cost-Control Rules
- Do not broad-scan the repository when the input already provides a file, class, function,
  module, service, or existing test-file anchor.
- Reuse an existing `implementation-cache.json` first. Only read markdown work artifacts or
	shared repository context for explicit missing facts.
- Read the smallest local slice first.
- Keep exact source-file and test-file inventories compact unless the requested behavior truly
	spans more boundaries.
- If the target behavior is too vague to identify one concrete anchor, ask for one scope anchor
  instead of exploring broadly.
- Do not run the entire test suite by default when a single-file, single-class, single-module,
  or filtered unit-test command exists.

## Stop Condition
STOP after unit-test creation, focused validation, and scoped coverage verification.

If required inputs remain missing after asking the user for them, STOP with:

```
CURRENT STAGE: Testcase Creation
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing required testcase inputs>
NEXT RECOMMENDED COMMAND: None within the pipeline
```

```
CURRENT STAGE: Testcase Creation
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <unit tests created or updated, executed, and coverage verified for the requested scope>
NEXT RECOMMENDED COMMAND: None within the pipeline
```