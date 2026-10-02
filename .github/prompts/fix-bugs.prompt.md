---
description: "Correct production-code defects found by /unit-testing or reported by the developer, then return to /unit-testing."
agent: bug-fixer
argument-hint: "Work ID with defects from /unit-testing, or new defects (ID + description; optional file, failing test, stack trace, story ID)"
---

# /fix-bugs

- Agent: bug-fixer. Skill: [defect-resolution](../skills/defect-resolution/SKILL.md).
- Input: a work ID with `OPEN` defects (no need to describe them), the validation handed off in this
  chat, or new defects with IDs and descriptions (QA or review comments count). Work folder: that
  item; else the story named in the message; else `.sdlc/work/<first BUG-ID>/`. Nothing given: ask once.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; for a story, status `BUG_FOUND` or
  `BUG_FIX_IN_PROGRESS`, or new defects in the message (`ANALYSIS_*`: recommend `/implement-story <ID>`).
- Start from: `work.json`, the last `/unit-testing` log entry, the ACs and Do Not Modify of `plan.md`.
- Result: defects resolved and logged; status `BUG_FIXED` (or `BUG_FOUND` when some need the developer).
- Next (manual): `/unit-testing <ID>` for full revalidation.
