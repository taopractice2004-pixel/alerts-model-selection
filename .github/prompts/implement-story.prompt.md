---
description: "Implement an approved story: execute ONLY the Implementation Plan of plan.md in production code."
agent: developer
argument-hint: "Story ID (add 'approved' to approve the plan, 'continue' to proceed despite a stale plan)"
---

# /implement-story

- Agent: developer. Skill: [production-implementation](../skills/production-implementation/SKILL.md).
- Input: story ID, or the story handed off in this chat; otherwise the only work folder with status
  `ANALYSIS_DRAFT` or `ANALYSIS_APPROVED`; otherwise ask. Optional: `approved`, `continue`.
- Requires: `.sdlc/work/<ID>/plan.md` and `work.json` (else recommend `/analyze-story <ID>`); status
  `ANALYSIS_DRAFT`, `ANALYSIS_APPROVED`, or `IMPLEMENTATION_IN_PROGRESS` (later: recommend
  `/unit-testing <ID>` or `/fix-bugs <ID>`).
- Start from: `work.json`, `plan.md`.
- Result: production changes recorded; status `IMPLEMENTATION_COMPLETE`.
- Next (manual): `/unit-testing <ID>`; for a plan or scope problem, `/analyze-story <ID>`.
