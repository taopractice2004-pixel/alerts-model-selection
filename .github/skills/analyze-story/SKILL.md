---
name: analyze-story
description: "Primary SDLC stage. Begin story work from user-provided story details by producing compact human-review artifacts and the authoritative machine-readable `implementation-cache.json`. Runs a deterministic pre-pass, classifies the story (SIMPLE / AMBIGUOUS / STALE_REPLAN), identifies the smallest exact file set, and uses the requirement-analyst agent. Use when starting or re-planning a story."
argument-hint: "<STORY-ID>"
user-invocable: true
disable-model-invocation: true
---

# Story Analysis

Authoritative procedure for the `/analyze-story` SDLC stage. This is the primary execution
path; the `prompt-analyze-story` prompt only delegates here.

## Purpose
Begin story work with compact story artifacts and a machine-readable implementation cache.
`implementation-cache.json` is the authoritative machine-readable output for later stages. The
markdown artifacts are thin human-review documents and must not repeat inventories already
owned by the cache.

## Applicable Agent
`requirement-analyst`. This skill is the only user-facing entry point and delegates its
requirement-analysis work to the `requirement-analyst` custom agent.

## Required Input
The user provides all requirement information directly. Ask for these if they are not already
provided:
- Story ID
- Story Description
- Acceptance Criteria

Do not fetch the story from Jira, MCP, Confluence, or any other external source.

If any of these is missing, STOP with `BLOCKED_MISSING_INFORMATION`.

## Shared Rules
Follow `.sdlc/framework/context-rules.md`, `.sdlc/framework/stage-rules.md`,
`.sdlc/config/effort-dial.md`, and the global rules in `.github/copilot-instructions.md`. Do
not duplicate those rules here.

## Procedure
1. Read `.sdlc/config/effort-dial.md` and any existing `.sdlc/work/<STORY-ID>/session.md`.
2. Follow `.sdlc/framework/context-rules.md`.
3. Perform a deterministic pre-pass before deeper reasoning:
   - identify likely touched layers
   - identify the smallest concrete scope anchors available from the story
   - identify likely exact source files, capped to the smallest useful set
   - identify likely exact tests, capped to the smallest useful set
   - identify build/test commands
   - select applicable standard ids from `.sdlc/context/standards-index.json` based on the
     touched file types/layers (for example `coding`, `backend-dotnet`, `api-rest`,
     `database`). Record ids, not rule text; the compact rules auto-apply through
     `.github/instructions/standards/*.instructions.md`.
4. Classify the story into one of three cases:
   - `SIMPLE`
   - `AMBIGUOUS`
   - `STALE_REPLAN`
5. Delegate the requirement-analysis work to the `requirement-analyst` custom agent, passing
   only the compact repository cache, the user-provided story inputs, and the deterministic
   pre-pass output.
6. Create or overwrite exactly once, never append, these files under `.sdlc/work/<STORY-ID>/`:
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
   - selected standards references, recorded as standard ids from
     `.sdlc/context/standards-index.json` (for example
     `selected_standards: ["coding", "backend-dotnet", "api-rest"]`)
   - validation, coverage, and reproduction commands
   - unresolved questions and missing facts
10. Keep compact limits unless the story truly requires broader scope:
    - exact source files: at most 5
    - exact test files: at most 3
    - selected standards: at most 5
11. For `SIMPLE` stories, keep `story-context.md`, `implementation-plan.md`, and
    `impact-map.md` to the smallest useful human-review summary. Do not copy long acceptance
    criteria blocks or duplicate the exact file inventory from the cache.

## Cost-Control Behavior
- `implementation-cache.json` is the single authoritative scope record; markdown artifacts stay
  thin and must not duplicate it.
- Honor the file-count caps above unless the story genuinely crosses more boundaries.
- Write `requirement-analysis.md` only for `AMBIGUOUS` cases or unresolved questions.
- Do not broad-scan the repository when the story already yields concrete anchors.
- Before any repository-wide search, honor `.sdlc/context/context-exclusions.json` (see
  `.sdlc/framework/context-rules.md`); do not scan generated, dependency, build, cache, or log
  paths listed there.

## Outputs
- `.sdlc/work/<STORY-ID>/session.md`
- `.sdlc/work/<STORY-ID>/story-context.md`
- `.sdlc/work/<STORY-ID>/implementation-plan.md`
- `.sdlc/work/<STORY-ID>/impact-map.md`
- `.sdlc/work/<STORY-ID>/implementation-cache.json`
- Optional: `.sdlc/work/<STORY-ID>/requirement-analysis.md`

## Stop Condition
STOP after the compact story cache is written. Do not invoke `/implement-story`; only
recommend it.

```
CURRENT STAGE: Story Analysis
STATUS: STAGE_PASSED | BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: <list>
SUMMARY: <compact story cache created>
NEXT RECOMMENDED COMMAND: /implement-story
```
