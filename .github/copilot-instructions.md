# Copilot Instructions

<!-- This file is attached to EVERY Copilot chat in this repository. Keep it short.
     The Project section is filled by /setup-repo-context and kept current by /refresh-repo-context. -->

## Project
- Alert management microservice built as an ASP.NET Core Web API on .NET 8 with EF Core SQL Server persistence, layered service/repository architecture, and xUnit-based tests.
- Repository map, pattern examples, testing context: `.sdlc/context/project-profile.md`.
  Coding rules: `.sdlc/context/standards-summary.md`. Build and test commands: `.sdlc/context/manifest.json`.

## Rules For Every Task
1. Follow existing project patterns. Find the closest existing example and copy its style before writing new code.
2. Before creating a new class, service, helper, validator, or utility, do one bounded search (by name and responsibility) for an existing equivalent and reuse or extend it.
3. Change only what the task needs. No unrelated refactoring, renaming, or reformatting.
4. Do not add frameworks, libraries, or packages without approval.
5. Never invent project rules, commands, or facts. Write `TO_BE_DISCOVERED`, `NOT_CONFIGURED`, or `NOT_AVAILABLE` instead.
6. Never hardcode secrets, credentials, or environment-specific values.

## Repository Context
- Use the cached context in `.sdlc/context/` before exploring the repository. Avoid broad scans.
- Search first (workspace, text, or file search), then read only the matching files and sections;
  expand only when information is still missing. Do not build a separate index.
- Get deterministic facts with direct read-only checks (file exists, current revision, changed
  files, known config values) instead of reasoning about them or searching for them.
- Respect `contextPolicy` in `.sdlc/context/manifest.json`: search `normalPaths` by default; open
  `readWhenRelevant` or `skipByDefault` paths only when the current task clearly requires them; never
  change `doNotModify` paths without developer approval.

## Git (read-only)
- Git may only inspect: `git status --short`, `git diff`, `git diff --stat`, `git diff --name-only`,
  `git rev-parse HEAD`, `git log`, `git show`. If Git is unavailable, skip it and continue.
- Never change Git state or configuration: no add, commit, reset, restore, checkout, switch, branch,
  merge, rebase, cherry-pick, revert, stash, clean, fetch, pull, push, remote, tag, init, or config;
  never touch credentials, hooks, SSH settings, or `.git/`.

## Running Commands
- Run the narrowest relevant check first (one project build, one test file, class, or filter); broader regression only after it passes.
- Use the commands and working directories in `.sdlc/context/manifest.json` (via `work.json`), with quiet output options.
- Never report PASS for something that was not run or verified; say `NOT_RUN` or `NOT_VERIFIED` with the reason.

## SDLC Pipeline
Stages are run manually by the developer: `/setup-repo-context`, `/refresh-repo-context`,
`/analyze-story`, `/implement-story`, `/unit-testing`, `/fix-bugs`. Stay within the approved scope of
the current stage. The story text is read only by `/analyze-story`; later stages use `plan.md`.
When a stage needs input from the developer, ask in the same conversation (use the askQuestions
tool when available) and continue the same stage after the answer; asking is not the end of a stage.
A stage stops when done and never starts another stage itself. Every stage ends with:
`CURRENT STAGE`, `STATUS` (`STAGE_PASSED`, `STAGE_FAILED`, `WAITING_FOR_HUMAN`, or
`BLOCKED_MISSING_INFORMATION`), `FILES CREATED/UPDATED`, `SUMMARY`, and `NEXT RECOMMENDED COMMAND`.
