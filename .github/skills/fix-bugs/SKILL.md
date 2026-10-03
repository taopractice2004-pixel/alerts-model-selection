---
name: fix-bugs
description: "Resolves production-code defects recorded by /unit-testing or reported by the developer: anchors and triages each defect, reproduces it with a failing regression test, fixes the root cause with the smallest change, checks for sibling occurrences, and records a short root-cause note. Never sets COMPLETE. Invoke directly as /fix-bugs <ID> [defects]."
argument-hint: "Work ID with defects from /unit-testing, or new defects (ID + description; optional file, failing test, stack trace, story ID)"
context: fork
user-invocable: true
disable-model-invocation: true
---

# /fix-bugs

This file is the complete, authoritative workflow for this stage. It runs in its own isolated
context (`context: fork`): use only this message and the files on disk, never earlier chat history.
Cross-stage state comes only from `.sdlc/` artifacts.

## Stage Contract
- Role: bug fixer. Change only defect-related production code and the regression test that proves
  each fix. Never set the story `COMPLETE`; full validation runs again in `/unit-testing`.
- Input: a work ID with `OPEN` defects in its `work.json` (no need to describe them), or new defects
  with IDs and descriptions (QA or review comments count). Work folder: that item; else the story
  named in the message; else `.sdlc/work/<first BUG-ID>/`. Nothing given: stop with
  `BLOCKED_MISSING_INFORMATION`.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; for a story, status `BUG_FOUND` or
  `BUG_FIX_IN_PROGRESS`, or new defects in the message (`ANALYSIS_*`: recommend `/implement-story <ID>`).
- Start from: `work.json`, the last `/unit-testing` log entry, the ACs and Do Not Modify of `plan.md`.
- Boundaries: commands: the regression test for each defect (`unit_test` command in `work.json`).
  Do not invoke another skill or custom agent.
- Result: defects resolved and logged; status `BUG_FIXED` (or `BUG_FOUND` when some need the developer).
- Return: the stage report defined in `.github/copilot-instructions.md`, then STOP.
  Next (manual): `/unit-testing <ID>` for full revalidation.

## Procedure

1. Set status `BUG_FIX_IN_PROGRESS`. Use the `defects` in `work.json` with their evidence, the last
   `/unit-testing` log entry, and for stories the Acceptance Criteria and Do Not Modify sections of
   `plan.md`. Add new defects from the message with outcome `OPEN`.
   No work folder yet (standalone bug): create `.sdlc/work/<first BUG-ID>/work.json` from the template
   (`work_type` = `bug`, `commands` from `manifest.json`) and `log.md` with only `# Log - <ID>`.
2. For each `OPEN` defect:
   1. **Anchor**: its evidence (failing test, error output, stack trace, file or class). Without one,
      one bounded search from the description (about 5 file reads); still nothing → `NEEDS_INFO`.
   2. **Triage** against the ACs or expected behavior:
      - `CONFIRMED_BUG`: continue
      - `EXPECTED_BEHAVIOR`: the code meets the ACs, so the test or report is wrong; cite the AC;
        change nothing (wrong tests are corrected in `/unit-testing`)
      - `OUT_OF_SCOPE`: a feature or change request; change nothing (re-plan with `/analyze-story`)
      - `NEEDS_INFO`: expected behavior unclear; do not guess; change nothing
   3. **Reproduce**: run the failing test, or write the regression unit test that shows the bug. It
      must FAIL for the reported reason. If no unit test can reproduce it, use the cheapest
      reproduction command and record the regression test `NOT_AVAILABLE (reason)`.
   4. **Fix** the root cause with the smallest production change, not a workaround. Stop with
      `WAITING_FOR_HUMAN` before shared contract or API changes, schema or migrations, a new
      dependency, shared or cross-cutting files, or Do Not Modify / `doNotModify` paths.
   5. **Verify**: the regression test now PASSES. 3 fix attempts per defect, else `NOT_FIXED` with the
      exact failure. `FIXED` only if it failed before and passes after.
   6. **Siblings**: one bounded search for the same faulty pattern; fix only inside this defect's
      scope, otherwise report it.
3. **Record.** Append the `/fix-bugs` entry from `.sdlc/templates/log.template.md`, one block per defect
   (triage; root cause in 2-3 lines: cause, why the fix is correct, what else it could affect; fix;
   regression test; siblings). In `work.json`: each `outcome`, `files_changed`, and status `BUG_FIXED`
   when no defect is `OPEN`, `NOT_FIXED`, or `NEEDS_INFO`; otherwise `BUG_FOUND` with
   `waiting_for_human` naming them. Never set `COMPLETE`.
