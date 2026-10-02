---
name: story-analysis
description: "Turns a pasted story (ID, description, acceptance criteria) into one complete plan.md plus work.json: numbered ACs, scope, impacted files, reuse candidates, contracts, behavior rules, implementation steps, validation plan, unit-test plan, AC-to-validation mapping, risks, and open questions. Use for /analyze-story."
user-invocable: false
---

# Story Analysis

## Procedure
1. **Re-analysis.** If `.sdlc/work/<ID>/` exists, read its `work.json` and `plan.md` and keep answers
   already given to open questions. If the old `analyzed_at_commit` is set, run
   `git diff --name-only <old analyzed_at_commit> HEAD -- <paths in the old Impacted Files>` and
   re-examine only those files plus anything the new story text changes. If the status is past
   `ANALYSIS_APPROVED`, warn that implementation already started; the developer decides what to redo.
2. **Read the story once** from the message. Everything later stages need goes into `plan.md`.
3. **Classify**: `SIMPLE`, or `AMBIGUOUS` (unclear or conflicting requirements).
4. **Cached context first**: `project-profile.md` (Architecture And Flow, Modules, Pattern Examples,
   Testing Context, Context Policy, Domain Glossary, Known Pitfalls, Shared Files), the section
   headings of `standards-summary.md`, and `commands` and `contextPolicy` in `manifest.json`. Explore
   the repository only where the cache does not answer, within `contextPolicy`: locate with search,
   read only relevant sections, about 10 file reads (`deep` effort may read more). If that is not
   enough, record an open question instead of reading more.
   Freshness signal (no refresh): if the manifest's latest commit differs from `git rev-parse HEAD`
   and `git diff --name-only <that commit> HEAD` or `git status --short` lists paths matching a
   section's `sources`, note in the summary that `/refresh-repo-context` is recommended.
   **Repository mode** (from `manifest.json`): `EXISTING`, or `GREENFIELD` with `INITIAL` maturity,
   changes steps 5-6 as marked "GREENFIELD". For GREENFIELD use the requirements summary (profile
   section 15) and Open Decisions (section 16); reread the requirement document only for a section
   the story needs that the summary lacks. If GREENFIELD is still `INITIAL` but real project code now
   exists, recommend `/refresh-repo-context` in the summary.
5. **Determine**:
   - production files to modify and create (about 5 in total unless the story is truly broader).
     GREENFIELD: only files that really exist go in Files to Modify ("None - greenfield repository"
     otherwise); new files go in Files to Create as `TO_CREATE` (path fixed by an approved structure
     or company standard) or `PROPOSED` (path suggested here). Never list a nonexistent file as existing.
   - one example file per kind of change, and existing code to reuse instead of creating new.
     GREENFIELD: `NOT_ESTABLISHED - no prior implementation exists` unless an earlier story created
     one; follow `standards-summary.md` and the approved plan instead.
   - GREENFIELD: architecture or technical decisions the story needs that no source settles (open
     decisions in profile section 16 included). Each is a blocking open question.
   - dependencies, risks, and stop points (Shared Files touched; possible shared contract, schema,
     or dependency changes)
   - the validation for `/unit-testing`: build and type check of the affected project, required
     lint, related regression tests, integration suite, coverage scope
   - the unit tests needed and how each AC will be validated
   - the `standards-summary.md` sections that apply
6. **Write `plan.md`** from `.sdlc/templates/plan.template.md` (overwrite fully):
   - Acceptance Criteria: `AC1 - ...`, one testable line each, same meaning as the story. Without
     explicit criteria, derive them and add a non-blocking question asking the developer to confirm.
   - Implementation Plan:
     - Contracts: exact shapes the story adds or changes (method and route, request and response
       fields, status codes, error codes or messages, signatures, DTO fields, config keys, UI props),
       following the conventions in `standards-summary.md`; `None` if nothing changes.
     - Behavior Rules: business rules, validation, edge cases, error handling, logging; one line
       each with its AC.
     - Steps: `[ACs] file -> class or method: change; follow <example file>`, plus `[wiring]` steps
       (dependency-injection registration, routes, config keys, feature flags). Production code
       only; no build, test, or validation steps.
     - Every AC appears in at least one step or behavior rule.
   - Validation Plan, Unit Test Plan (actual unit tests only), AC-to-Validation Mapping: every AC
     gets a row with a type: `UNIT_TEST`, `UI_COMPONENT_TEST` (only with an existing component-test
     setup), `BUILD_CHECK`, or `INTEGRATION_FUNCTIONAL` (existing automated suite, or a manual
     check). Prefer an automated type. Expected results come from Contracts and Behavior Rules
     (same codes, messages, and values).
   - Do Not Modify: include the `contextPolicy` `doNotModify` paths near the story's files.
   - Approval Status: `DRAFT`, or `WAITING_FOR_HUMAN` when a blocking question exists.
   - Keep it short; for `SIMPLE` stories most sections are a few lines.
7. **Write `work.json`** from `.sdlc/templates/work.template.json` (overwrite fully): `status` =
   `ANALYSIS_DRAFT`; `waiting_for_human` = the blocking question or null; `analyzed_at_commit` =
   `git rev-parse HEAD` (null without git); `selected_standards` (section names); `coverage_goal`
   from profile section 7; `commands` copied from `manifest.json`, narrowed to this story.
8. **Log.** Create `log.md` with only `# Log - <ID>` if missing, then append the `/analyze-story`
   entry from `.sdlc/templates/log.template.md` (record re-analysis and changed files).
