---
description: "Analyze one story, pasted as text, into one complete plan.md and work.json. No code, builds, or tests."
agent: analyst
argument-hint: "Story ID, then the story text: title, description, acceptance criteria (add 'deep' for risky stories)"
---

# /analyze-story

- Agent: analyst. Skill: [story-analysis](../skills/story-analysis/SKILL.md).
- Input: story ID and the story text (title, description, acceptance criteria) in this message;
  optional `deep`. The message is the only requirements source. Missing ID or text: ask once.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; otherwise recommend `/setup-repo-context`.
- Start from: `.sdlc/context/`; an existing `.sdlc/work/<ID>/` when re-analyzing.
- Result: `plan.md` (`DRAFT`, or `WAITING_FOR_HUMAN` for blocking questions), `work.json`
  (`ANALYSIS_DRAFT`), `log.md` entry.
- Next (manual): review `plan.md`, then "Approve plan and implement" or `/implement-story <ID> approved`.
