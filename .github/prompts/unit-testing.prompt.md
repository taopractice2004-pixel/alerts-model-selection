---
description: "Validate a story or bug fix: build, tests, regression, lint, coverage, and every acceptance criterion."
agent: tester
argument-hint: "Work ID, or a target file, class, or function for standalone tests"
---

# /unit-testing

- Agent: tester. Skill: [story-validation](../skills/story-validation/SKILL.md).
- Input: work ID, or the work handed off in this chat; otherwise the only work folder with status
  `IMPLEMENTATION_COMPLETE` or `BUG_FIXED`; or a target file, class, or function with no work folder
  (standalone test task). Otherwise ask once.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; status `IMPLEMENTATION_COMPLETE`,
  `BUG_FIXED`, `UNIT_TESTING_IN_PROGRESS`, or `COMPLETE` (earlier: recommend `/implement-story <ID>`;
  `BUG_FOUND` or `BUG_FIX_IN_PROGRESS`: recommend `/fix-bugs <ID>`).
- Start from: `work.json`, the last `log.md` entries, the validation sections of `plan.md`.
- Result: build, test, and AC results logged; status `COMPLETE`, `BUG_FOUND`, or unchanged with
  `waiting_for_human`.
- Next (manual): none when `COMPLETE`; `/fix-bugs <ID>` for production defects; `/analyze-story <ID>`
  for a plan issue.
