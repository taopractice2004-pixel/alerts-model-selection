---
description: "Refresh only the parts of the repository context whose sources changed since the last build or refresh."
agent: agent
tools: ['search', 'read', 'edit', 'execute']
---

# /refresh-repo-context

Use only when structure, architecture, standards, commands, or other cached context changed.

- Agent: built-in agent (tools above). Skill: [repo-context-extraction](../skills/repo-context-extraction/SKILL.md), refresh mode.
- Input: optional names of sections to refresh.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; otherwise recommend `/setup-repo-context`.
- Start from: `.sdlc/context/manifest.json`.
- Result: only the stale context refreshed; `refreshedAtCommit` updated.
- Next (manual): `/analyze-story <ID> <story text>`.
