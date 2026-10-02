---
description: "Build the reusable repository context once, for an existing repository or a new (greenfield) one."
agent: agent
tools: ['search', 'read', 'edit', 'execute', 'vscode/askQuestions']
---

# /setup-repo-context

Run once per repository (or for an intentional full rebuild).

- Agent: built-in agent (tools above). Skill: [repo-context-extraction](../skills/repo-context-extraction/SKILL.md), setup.
- Mode: if the message does not already say which, ask in this same conversation (with the
  askQuestions tool when available) and continue setup as soon as the developer answers. Ask exactly:

  ```text
  Select repository mode:

  1. New Repository
     Starting a new feature/project where application code or established project patterns do not yet exist.

  2. Existing Repository
     Application code already exists and development will continue using the existing architecture and patterns.
  ```
  1 = `GREENFIELD`, 2 = `EXISTING`.
- GREENFIELD input: the requirement document or its workspace path (ask the same way if not given), plus any
  stack or architecture decisions the developer states. Unreadable document: stop with
  `BLOCKED_MISSING_INFORMATION`.
- Start from: the starter files in `.sdlc/context/`.
- Result: `project-profile.md`, `standards-summary.md`, and `manifest.json` (status `BUILT`, with
  `repositoryMode`); the Project line in `copilot-instructions.md`. Setup never creates application code.
- Next (manual): review `.sdlc/context/` (GREENFIELD: resolve Open Decisions in section 16), then
  `/analyze-story <ID> <story text>`.
