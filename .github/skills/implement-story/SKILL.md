---
name: implement-story
description: "Primary SDLC stage. Implement an already-analyzed story from its compact story cache using the smallest required context. Reads implementation-cache.json first, then only the exact files it names, uses the developer agent, runs the narrowest validation, and updates changes.md and session.md. Use after /analyze-story has produced the story cache."
argument-hint: "<STORY-ID>"
user-invocable: true
disable-model-invocation: true
---

# Story Implementation

Authoritative procedure for the `/implement-story` SDLC stage. This is the primary execution
path; the `prompt-implement-story` prompt only delegates here.

## Purpose
Implement the approved story using the smallest required context.

## Applicable Agent
`developer`. This skill is the only user-facing entry point and delegates its implementation
work to the `developer` custom agent.

## Required Input
Story ID. If the only in-progress story is not inferable, ask for the Story ID.

## Prerequisites
The story cache must exist:
- `.sdlc/work/<STORY-ID>/session.md`
- `.sdlc/work/<STORY-ID>/story-context.md`
- `.sdlc/work/<STORY-ID>/implementation-plan.md`
- `.sdlc/work/<STORY-ID>/impact-map.md`
- `.sdlc/work/<STORY-ID>/implementation-cache.json`

If these files are missing, STOP and recommend `/analyze-story`.

## Shared Rules
Follow `.sdlc/framework/context-rules.md`, `.sdlc/framework/stage-rules.md`, the applicable
`.github/instructions/*.instructions.md`, and the global rules in
`.github/copilot-instructions.md`. Do not duplicate those rules here.

## Procedure
1. Read, in this order:
   - `implementation-cache.json`
   - `session.md`
   - exact source files and tests listed in `implementation-cache.json`
2. Read `implementation-plan.md` only if implementation intent or validation rationale is
   missing from `implementation-cache.json`.
3. Read `story-context.md` only if business constraints, work relationship, or tracker context
   is missing from `implementation-cache.json`.
4. Read `impact-map.md` only if adjacent dependencies or out-of-scope boundaries are missing
   from `implementation-cache.json`.
5. Read `requirement-analysis.md` only if the story case is `AMBIGUOUS`, unresolved questions
   remain, or the cache is incomplete.
6. Read `.sdlc/context/repo-profile.md` or `.sdlc/context/standards-summary.md` only if
   `repo_context_fallback_needed` is true or the cache explicitly records a missing fact.
   For standards, rely on the compact `.github/instructions/standards/*.instructions.md` files
   that auto-apply to the touched files and on the `selected_standards` ids in the cache; read a
   full `standards/*.md` file only when an exact rule or missing detail is required. Never
   reread the entire `standards/` folder.
7. If an exact file is missing from the cache but the slice is otherwise clear, patch only the
   missing cache field from the smallest nearby read, then continue.
8. Do not broad-scan the repository when the cache already identifies exact files or anchors.
9. Delegate the implementation work to the `developer` custom agent, passing only the minimal
   story cache, the relevant instructions, and the exact touched files.
10. Run the narrowest validation commands recorded in `implementation-cache.json`. Do not run a
    full repository build or test suite when a focused check exists for the slice.
11. Update these files under `.sdlc/work/<STORY-ID>/`:
    - `changes.md`
    - `session.md`
12. Update `implementation-cache.json` and `impact-map.md` only if the actual touched files,
    tests, or validation commands changed during implementation.

## Cost-Control Behavior
- Cache first, then exact files only; do not reread the tracker or broad documentation.
- Do not rescan the repository when the cache already names exact files or anchors.
- For standards, rely on the `selected_standards` ids and the compact
  `.github/instructions/standards/*.instructions.md` files that auto-apply to the touched files;
  read a full `standards/*.md` file only for an exact rule or missing detail, and never reread
  the entire `standards/` folder.
- Run the narrowest validation available, not the full suite.
- Before any repository-wide search, honor `.sdlc/context/context-exclusions.json` (see
  `.sdlc/framework/context-rules.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.
- Correct the cache only when the actual touched slice changed.

## Outputs
- Source code changes
- `.sdlc/work/<STORY-ID>/changes.md`
- `.sdlc/work/<STORY-ID>/session.md`
- Optional cache corrections in `implementation-cache.json` and `impact-map.md`

## Stop Condition
STOP after implementation and validation. Do not invoke `/unit-testing`; recommend it only
where appropriate.

```
CURRENT STAGE: Story Implementation
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <code implemented from compact story cache>
NEXT RECOMMENDED COMMAND: /unit-testing only when unit-test coverage for the implemented slice is wanted; otherwise None within the pipeline
```
