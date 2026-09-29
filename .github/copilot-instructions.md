# Copilot Instructions — SDLC Automation Framework

This repository contains a **reusable, manual, state-driven SDLC pipeline** built on GitHub
Copilot customization features (agents, prompts, instructions). These rules apply globally to
every agent and prompt in this framework.

## Universal Rules

1. **Always inspect existing project patterns before modifying code.** Use
   `.sdlc/context/repo-profile.md` and `.sdlc/context/repository-map.md`, plus the actual
   neighboring source, before writing new code.
2. **Never invent project-specific rules.** If a standard, convention, or policy is not
   discoverable, mark it `NOT_AVAILABLE`, `NOT_CONFIGURED`, `TO_BE_DISCOVERED`, or
   `TO_BE_CONFIGURED` — do not fabricate one.
3. **Prefer cached/reusable context before expensive rereads.** Follow the context
   resolution order in `.sdlc/framework/context-rules.md` before rereading Jira, full
   documents, or rescanning the repository.
4. **Implement only the requested story scope.** Avoid unrelated refactoring.
5. **This packaged pipeline covers repository setup, story analysis, implementation, focused bug
   fixing, and focused unit-test creation only.** Do not assume integration testing, PR,
   review, merge, deployment, or QA stages exist in this repository unless they are added back
   explicitly.
6. **Update workflow state after each stage.** Every stage writes to
    `.sdlc/work/<ID>/session.md`.
7. **Preserve traceability** from repository context → requirement analysis, bug context, or
   testcase scope → implementation and validation through the artifacts under `.sdlc/` and
   `.sdlc/work/<ID>/`.
8. **Stop after each manually invoked stage.** Never chain stages automatically.
9. **Recommend, never automatically invoke, the next SDLC command.** Every stage ends with
    the standard outcome block (`CURRENT STAGE`, `STATUS`, `FILES CREATED/UPDATED`,
    `SUMMARY`, `NEXT RECOMMENDED COMMAND`) and one of `STAGE_PASSED`, `STAGE_FAILED`,
    `WAITING_FOR_HUMAN`, `BLOCKED_MISSING_INFORMATION`.
10. **No orchestrator.** No prompt or agent triggers another SDLC command on its own.
11. **Technology-agnostic by design.** Nothing in this framework hardcodes a language,
    framework, project name, Jira ID, repository, branch, test framework, build command, or
   company-specific rule. All of that is discovered per-repository via `/setup-repo-context` /
   `/refresh-repo-context` or supplied per story, bug, or unit-test task via `/analyze-story`,
   `/fix-bugs`, or `/create-testcases`.

## Where to Look

- Full lifecycle and command sequencing: `.sdlc/framework/workflow.md`
- Per-stage prerequisites/outputs: `.sdlc/framework/stage-rules.md`
- Context reuse/caching rules: `.sdlc/framework/context-rules.md`
- Repository-level context: `.sdlc/context/`
- Per-work-item runtime state: `.sdlc/work/<ID>/`
- Reusable templates: `.sdlc/templates/`
- Domain instructions: `.github/instructions/`
- Agents: `.github/agents/`
- Commands: `.github/prompts/`

See the root [README.md](../README.md) for a full walkthrough.
