---
name: refresh-repo-context
description: "Refreshes only the parts of the cached repository context in .sdlc/context whose sources changed since the last build or refresh (structure, architecture, standards, commands, applyTo paths), and promotes a GREENFIELD context to ESTABLISHED once real code exists. Invoke directly as /refresh-repo-context."
argument-hint: "Optional: names of sections to refresh"
context: fork
user-invocable: true
disable-model-invocation: true
---

# /refresh-repo-context

Use only when structure, architecture, standards, commands, or other cached context changed.
This file is the complete, authoritative workflow for this stage. It runs in its own isolated
context (`context: fork`): use only this message and the files on disk, never earlier chat history.

## Stage Contract
- Input: optional names of sections to refresh.
- Requires: `.sdlc/context/manifest.json` with `status: BUILT`; otherwise stop with
  `BLOCKED_MISSING_INFORMATION` and recommend `/setup-repo-context`.
- Start from: `.sdlc/context/manifest.json`.
- Boundaries: write only `.sdlc/context/`, the Project line of `.github/copilot-instructions.md`,
  and the `applyTo` lines of `.github/instructions/*.instructions.md`. Never edit application code;
  never build or run tests. Do not invoke another skill or custom agent.
- Result: only the stale context refreshed; `refreshedAtCommit` updated.
- Return: the stage report defined in `.github/copilot-instructions.md`, then STOP.
  Next (manual): `/analyze-story <ID> <story text>`.

## Rules
- Build a MAP of the repository, not a copy. Never list every file, class, or method; never paste
  source or document bodies.
- One home per fact: repository facts in `project-profile.md`, coding rules in
  `standards-summary.md`, commands, context policy, and source paths in `manifest.json`.
- Read efficiently: only the changed paths and the files that define the stale sections.
- Never record secrets, passwords, tokens, connection strings, or other sensitive values; record
  configuration file names and environment variable names only.
- Write `TO_BE_DISCOVERED` or `NOT_APPLICABLE` instead of guessing.
- Never overwrite the developer-maintained sections 10-13 of `project-profile.md` (Domain Glossary,
  Known Pitfalls, Story Conventions, Shared Files).
- `contextPolicy` keeps `standards/` in `skipByDefault` and `doNotModify`, and the pipeline's own
  files (`.github/skills/`, `.sdlc/templates/`, `.sdlc/work/`) in `skipByDefault`.

## Procedure
1. Take `refreshedAtCommit`, or `builtAtCommit` if never refreshed. Changed paths = the output of
   `git diff --name-only <commit>..HEAD` plus the paths in `git status --short` (uncommitted
   changes count). Without Git or a commit, refresh only the sections the developer names.
2. A section is stale when a changed path matches one of its `sources`; sections the developer names
   are stale too. `commands` are also stale when a `/unit-testing` log entry since the last refresh
   verified or corrected a command that `manifest.json` still lists as `NOT_RUN (unverified)`. If nothing is stale, stop and report that nothing needed refreshing.
3. Refresh only stale parts, with the Rules above:
   - `contextPolicy` (and profile section 9) for changed paths it does not yet cover: new top-level
     folders, new build output, new generated or vendor code
   - stale sections of `project-profile.md` and `standards-summary.md`
   - `commands`: set to `VERIFIED` (or corrected) every command a `/unit-testing` log entry records as
     passed or discovered; when build, package, or test files changed, re-discover only the affected
     commands without running them (`NOT_RUN (unverified)`)
   - `applyTo` paths and the Project line, when folder structure changed
4. **GREENFIELD with `contextMaturity` `INITIAL`** once real project code exists: read (as a reference
   file, not as an invoked skill) the EXISTING setup steps in `.github/skills/setup-repo-context/SKILL.md`
   and apply them to the parts that now exist (structure, modules, pattern examples from the real
   files, testing context, commands, `applyTo` paths). Keep `[REQ]` facts and resolved or open
   decisions in sections 15-16. Set `contextMaturity` `ESTABLISHED` when patterns, build, and tests
   exist (otherwise keep `INITIAL`). Never change `repositoryMode`; never edit application code.
5. Update `manifest.json`: `refreshedAtUtc`, `refreshedAtCommit`, updated `commands`, changed
   `sources`, `contextMaturity`, `establishment`.
